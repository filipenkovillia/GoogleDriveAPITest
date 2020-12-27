using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace GoogleDriveAPITest
{
    class Program
    {
        private static string[] Scopes = { DriveService.Scope.Drive };
        private static string ApplicationName = "GoogleDriveAPITest";
        private static string FolderId = "1bf9mxQsfS1iUObXsR5HNKfS-WYX3z9ai";
        private static string _fileName = "testFile";
        private static string _filePath = @"D:\My Projects\Downloads.rar";
        private static string _contentType = "application/zip";
        private static string _downloadFilePath = @"D:\My Projects\saved.rar";

        static void Main(string[] args)
        {
            Console.WriteLine("Create Credentials");
            UserCredential credential = GetUserCredential();

            Console.WriteLine("Get service");
            DriveService service = GetDriveService(credential);

            Console.WriteLine("Uploading file");
            var fileId = UploadFileDrive(service, _fileName, _filePath, _contentType);

            Console.WriteLine("Download file");
            DownloadFileFromDrive(service, fileId, _downloadFilePath);

            Console.WriteLine("End");
            /*
            IList<Google.Apis.Drive.v3.Data.File> files = service.Files.List().Execute().Files;

            foreach(var file in files)
            {
                Console.WriteLine("File title: {0}, id: {1}", file.Name, file.Id);
            }
            */

            Console.ReadLine();
        }

        private static void DownloadFileFromDrive(DriveService service, string fileId, string filePath)
        {
            var request = service.Files.Get(fileId);

            using (var memoryStream = new MemoryStream())
            {
                request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Downloading:
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        case DownloadStatus.Completed:
                            Console.WriteLine("Download Complete");
                            break;
                        case DownloadStatus.Failed:
                            Console.WriteLine("Download Failed");
                            break;
                    }
                };

                request.Download(memoryStream);

                using(var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }
            }
        }

        private static DriveService GetDriveService(UserCredential credential)
        {
            return new DriveService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

        }

        private static UserCredential GetUserCredential()
        {
            using (var stream = new FileStream("D:\\My Projects\\client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string creadPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                creadPath = Path.Combine(creadPath, "driveApiCredentials", "drive-credentials.json");

                return GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "User",
                    CancellationToken.None,
                    new FileDataStore(creadPath, true)).Result;
            }
        }

        private static string UploadFileDrive(DriveService service, string fileName, string filePath, string contentType)
        {
            var fileMetaData = new Google.Apis.Drive.v3.Data.File();
            fileMetaData.Name = fileName;
            fileMetaData.Parents = new List<string> { FolderId };

            FilesResource.CreateMediaUpload request;

            using(var stream = new FileStream(filePath, FileMode.Open))
            {
                request = service.Files.Create(fileMetaData, stream, contentType);
                request.Upload();
            }

            var file = request.ResponseBody;

            return file.Id;
        }
    }
}
