# AWS Image Storage CDK Stack

This CDK stack utilizes a Unity WebGL Application to facilitate lambda calls for User Image management. Users are managed by the AWS Cognito User Pool service. The Unity WebGL Application project can be found [Here](https://github.com/jonasenglish/S3BucketUpload)

This stack can be created using your AWS account after using AWS Configure in a CLI of your choice. You will need to have installed Node JS and the AWS CDK. AWS CDK can be installed via the following line:
`npm install -g aws-cdk`

Then, navigate to the project folder and run `cdk deploy`. The frontend Unity WebGL app is bundled in this [ZIP File](https://github.com/jonasenglish/AWS-Image-Storage-CDK-Stack/tree/main/webgl) and is uploaded and extracted during deployment.

After deployment the site will be accessible via the `UnityInfraStack.HostedUiLoginUrl` URL in the deployment outputs.
