


using System;
using System.Collections.Specialized;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


//Написать консольное приложение, которое будет выполнять следующие действия:

//Подключаться к FTP-серверу.
//Загружать все файлы из указанной директории (например, /images/) на FTP-сервере.
//Перед началом загрузки файлов, программа должна запрашивать у пользователя директорию на его компьютере, куда файлы будут сохранены.
//Программа должна показывать прогресс скачивания каждого файла (в процентах).
//После завершения загрузки всех файлов программа должна вывести отчёт о том, сколько файлов было скачано и общий размер загруженных данных.



string ftpUrl = "ftp://ftp.intel.com";
string ftpUrl_Images = $"{ftpUrl}/images";
string username = "anonymous";
string password = "test@test.gmail.com";

long totalSize = 0;
int filesDownloaded = 0;

//Console.WriteLine("Введите локальную директорию для сохранения файлов:");
//string localDirectory = Console.ReadLine();

string localDirectory = @"\test";

if (!Directory.Exists(localDirectory))
{
    Directory.CreateDirectory(localDirectory);
}


var DataFtp = new List<string>();
var DataFtpImages = new List<string>();

var fileInfo = new List<FileInfo>();
try
{

    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
    request.Method = WebRequestMethods.Ftp.ListDirectory;


    request.Credentials = new NetworkCredential(username, password);


    using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
    {
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            //Console.WriteLine(line);
            DataFtp.Add(line);
        }
    }



    if (DataFtp.Contains("images"))
    {



        FtpWebRequest listRequest = (FtpWebRequest)WebRequest.Create(ftpUrl_Images);
        listRequest.Method = WebRequestMethods.Ftp.ListDirectory;


        listRequest.Credentials = new NetworkCredential(username, password);


        using (FtpWebResponse listResponse = (FtpWebResponse)listRequest.GetResponse())
        using (StreamReader reader = new StreamReader(listResponse.GetResponseStream()))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                //Console.WriteLine(line);
                DataFtpImages.Add(line);
            }
            Console.WriteLine($"Список файлов получен. Всего файлов: {DataFtpImages.Count}");

        }





        foreach (var fileName in DataFtpImages)
        {
            string remoteFileUrl = $"{ftpUrl_Images}/{fileName}";
            string localFilePath = Path.Combine(localDirectory, fileName);


            FtpWebRequest requestSizeFile = (FtpWebRequest)WebRequest.Create(remoteFileUrl);
            requestSizeFile.Method = WebRequestMethods.Ftp.GetFileSize;


            requestSizeFile.Credentials = new NetworkCredential(username, password);



            using (FtpWebResponse response = (FtpWebResponse)requestSizeFile.GetResponse())
            {
                long fileSize = response.ContentLength;
                var imgInfo = new FileInfo(fileName, fileSize);
                fileInfo.Add(imgInfo);
                //Console.WriteLine($"Размер файла: {fileSize} байт");
            }

        }

        Task[] tasks = new Task[fileInfo.Count];
        for (int i = 0; i < fileInfo.Count; i++)
        {
            var file = fileInfo[i]; 
            tasks[i] = DownloadFile(file, i);
        }
        Task.WaitAll(tasks);
        Console.WriteLine($"\nЗагрузка завершена. Всего файлов: {filesDownloaded}, общий размер: {totalSize} байт.");




        async Task DownloadFile(FileInfo file, int index)
        {
            string remoteFileUrl = $"{ftpUrl_Images}/{file.name}";
            string localFilePath = Path.Combine(localDirectory, file.name);


            FtpWebRequest downloadRequest = (FtpWebRequest)WebRequest.Create(remoteFileUrl);
            downloadRequest.Method = WebRequestMethods.Ftp.DownloadFile;


            downloadRequest.Credentials = new NetworkCredential(username, password);



            using (FtpWebResponse response = (FtpWebResponse)downloadRequest.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (FileStream fs = new FileStream(localFilePath, FileMode.Create))
            {
                byte[] buffer = new byte[4096];
                long bytesRead = 0;
                int size;

                while ((size = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fs.Write(buffer, 0, size);
                    bytesRead += size;
                    int percent = (int)((bytesRead * 100) / file.size);
                   
                    var separator = new string('*', percent);

                    await UpdateProgressBar(percent, file.name, index);

                }
               
                totalSize += bytesRead;
                filesDownloaded++;
            
                Console.WriteLine("");
            }

        }



        static async Task UpdateProgressBar(int percent, string name,int index)
        {
            string separator = new string('*', percent);
            int maxProgress = 100;

            Console.SetCursorPosition(5, 5 * index);
            Console.Write($"Загрузка {name}:");

            Console.SetCursorPosition(5, 5 * index + 1);
            Console.WriteLine(new string('-', maxProgress + 2));

            Console.SetCursorPosition(5, 5 * index + 2);
            Console.Write("|");
            Console.Write(separator);
            Console.Write(new string(' ', maxProgress - percent));
            Console.WriteLine("|");

            Console.SetCursorPosition(5, 5 * index + 3);
            Console.WriteLine(new string('-', maxProgress + 2));

            await Task.Delay(100); 
        }
    }
}
catch (WebException ex)
{
    Console.WriteLine($"Произошла ошибка: {ex.Message}");

    if (ex.Response is FtpWebResponse ftpResponse)
    {
        Console.WriteLine($"Код состояние: {ftpResponse.StatusCode}");
        Console.WriteLine($"Message: {ftpResponse.StatusDescription}");
    }
}


record FileInfo(string name, long size);


