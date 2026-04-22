using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Notifications;
using Constructs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SysEnv = System.Environment;

namespace UnityInfra.Constructs;

public sealed class LambdaConstruct : Construct
{
    public Function UploadImage { get; }
    public Function DeleteImage { get; }
    public Function ViewImages { get; }
    public Function ProcessImage { get; }

    public Alias UploadImageAlias { get; }
    public Alias DeleteImageAlias { get; }
    public Alias ViewImagesAlias { get; }
    public Alias ProcessImageAlias { get; }

    public LambdaConstruct(Construct scope, string id, Bucket imageBucket) : base(scope, id)
    {
        var lambdaCode = Code.FromAsset(PublishLambdasLocally());

        Function CreateLambda(string functionId, string handler) =>
            new(this, functionId, new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Code = lambdaCode,
                Handler = handler,
                SnapStart = SnapStartConf.ON_PUBLISHED_VERSIONS,
                Timeout = Duration.Seconds(20),
                Environment = new Dictionary<string, string>
                {
                    ["IMAGE_BUCKET_NAME"] = imageBucket.BucketName
                }
            });

        Alias CreateAlias(string aliasId, Function function) =>
            new(this, aliasId, new AliasProps
            {
                AliasName = "live",
                Version = function.CurrentVersion
            });

        UploadImage = CreateLambda("UploadImage", "Lambdas::Lambdas.UploadImage::Handle");
        DeleteImage = CreateLambda("DeleteImage", "Lambdas::Lambdas.DeleteImage::Handle");
        ViewImages = CreateLambda("ViewImages", "Lambdas::Lambdas.ViewImages::Handle");
        ProcessImage = CreateLambda("ProcessImage", "Lambdas::Lambdas.ProcessImage::Handle");

        UploadImageAlias = CreateAlias("UploadImageAlias", UploadImage);
        DeleteImageAlias = CreateAlias("DeleteImageAlias", DeleteImage);
        ViewImagesAlias = CreateAlias("ViewImagesAlias", ViewImages);
        ProcessImageAlias = CreateAlias("ProcessImageAlias", ProcessImage);

        imageBucket.GrantReadWrite(UploadImageAlias);
        imageBucket.GrantReadWrite(DeleteImageAlias);
        imageBucket.GrantRead(ViewImagesAlias);
        imageBucket.GrantReadWrite(ProcessImageAlias);

        imageBucket.AddEventNotification(
            EventType.OBJECT_CREATED_PUT,
            new LambdaDestination(ProcessImageAlias),
            new NotificationKeyFilter { Prefix = "uploads/" });
    }

    private static string ResolveLambdasPath()
    {
        var probeDirs = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        foreach (var startDir in probeDirs)
        {
            var dir = new DirectoryInfo(startDir);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "Lambdas");
                if (File.Exists(Path.Combine(candidate, "Lambdas.csproj")))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate Lambdas/Lambdas.csproj.");
    }

    private static string PublishLambdasLocally()
    {
        var lambdasDir = ResolveLambdasPath();
        var projectFile = Path.Combine(lambdasDir, "Lambdas.csproj");
        var publishDir = Path.Combine(lambdasDir, "bin", "Release", "net8.0", "publish");

        Directory.CreateDirectory(publishDir);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{projectFile}\" -c Release -o \"{publishDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new Exception("Failed to start dotnet publish.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"dotnet publish failed with exit code {process.ExitCode}.{SysEnv.NewLine}{stdout}{SysEnv.NewLine}{stderr}");
        }

        if (!File.Exists(Path.Combine(publishDir, "Lambdas.dll")))
        {
            throw new FileNotFoundException($"Publish succeeded but Lambdas.dll was not found in {publishDir}.");
        }

        return publishDir;
    }
}