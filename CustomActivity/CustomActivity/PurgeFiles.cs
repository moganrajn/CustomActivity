using System;
using System.IO;
using System.Activities;
using System.ComponentModel;
using System.IO.Compression;

namespace CustomActivity
{

    public enum typeOfPurge
    {
        Delete = 1,
        Copy = 2,
        Move = 3,
        ZipAndMove = 4
    }

    public enum includeSubFolder
    {
        True = 1,
        False = 0
    }

    public class PurgeFiles : CodeActivity
    {

        [RequiredArgument]
        [Category("From")]
        public InArgument<string> SourcePath { get; set; }
            
        [RequiredArgument]
        [Category("OlderThan")]
        public InArgument<int> InDays { get; set; }

        [Category("PurgeType")]
        public typeOfPurge Action { get; set; }

        [Category("To")]
        public InArgument<string> DestinationPath { get; set; }

        [Category("Input")]
        public includeSubFolder IncludeSubFolder { get; set; }

        [Category("Output")]
        public OutArgument<bool> Result { get; set; }

        [Category("Output")]
        public OutArgument<string> ErrorMessage { get; set; }

        public PurgeFiles()
        {
            Action = typeOfPurge.Delete;
            IncludeSubFolder = includeSubFolder.False;
        }

        protected override void Execute(CodeActivityContext context)
        {
            try
            {
                //Read input values
                string sourceDirectory = SourcePath.Get(context);
                int cutOfNumber = (int)InDays.Get(context);
                string distinationDirectory = DestinationPath.Get(context);
                int actionType = this.Action.GetHashCode();
                bool isRequiredToPerformSubFolder = this.IncludeSubFolder.GetHashCode() == 1 ? true : false;

                bool Flag = false;

                //Make sure source folder path is valid
                if (Directory.Exists(sourceDirectory))
                {
                    string[] files = Directory.GetFiles(sourceDirectory);

                    switch (actionType)
                    {
                        case 1:
                            Flag = DeleteFiles(files, cutOfNumber, sourceDirectory, isRequiredToPerformSubFolder);
                            break;
                        case 2:
                            Flag = CopyFiles(files, cutOfNumber, sourceDirectory, distinationDirectory, isRequiredToPerformSubFolder);
                            break;
                        case 3:
                            Flag = MoveFiles(files, cutOfNumber, sourceDirectory, distinationDirectory, isRequiredToPerformSubFolder);
                            break;
                        case 4:
                            Flag = CompressToZip(files, cutOfNumber, sourceDirectory, distinationDirectory, isRequiredToPerformSubFolder);
                            break;
                    }
                }

                if (!Flag)
                {
                    ErrorMessage.Set(context, "None of the files found in giving criteria 'file CreatedDate < " + DateTime.Now.AddDays(-cutOfNumber).Date.ToString("MM-dd-yyyy") + "'");
                }

                Result.Set(context, Flag);

            }
            catch (Exception ex)
            {

                Result.Set(context, false);
                ErrorMessage.Set(context, ex.Message.ToString());
            }
        }


        private bool CompressToZip(string[] fileList, int cutOfDays, string sourceDirectory, string distinationFilePath, bool isRequiredToPerformSubFolder)
        {
            bool isSuccess = false;
            string zipfileFullPath = string.Empty;

            if (string.Empty != distinationFilePath)
            {
                if (!Directory.Exists(distinationFilePath))
                {
                    Directory.CreateDirectory(distinationFilePath);
                }
            }

            //Name zip file 
            zipfileFullPath = Path.Combine(distinationFilePath, DateTime.Now.ToString("MM-dd-yyyy_hh-mm-ss") + ".zip");

            if (!isRequiredToPerformSubFolder)
            {
                // Create and open a new ZIP file
                var zip = ZipFile.Open(zipfileFullPath, ZipArchiveMode.Create);

                foreach (var file in fileList)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime.Date < DateTime.Now.AddDays(-cutOfDays).Date)
                    {
                        // Add the entry for each file
                        zip.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
                        File.Delete(file);
                        isSuccess = true;
                    }
                }

                // Dispose of the object 
                zip.Dispose();
            }
            else
            {
                ZipFile.CreateFromDirectory(sourceDirectory, zipfileFullPath);

                DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirectory);

                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                {
                    dir.Delete(true);
                }
                isSuccess = true;
            }

            return isSuccess;
        }

        //private bool CompressToZip(string[] fileList, int cutOfDays, string sourceDirectory, string distinationFilePath, bool isRequiredToPerformSubFolder)
        //{
        //    bool isSuccess = false;
        //    bool isFileExists = false;
        //    string zipfileFullPath = string.Empty;

        //    //Name zip file 
        //    zipfileFullPath = distinationFilePath + "\\" + DateTime.Now.ToString("MM-dd-yyyy_hh-mm-ss") + ".zip";

        //    //Make sure atleast one file matching criteria
        //    foreach (string file in fileList)
        //    {
        //        FileInfo fileInfo = new FileInfo(file);

        //        if (fileInfo.CreationTime.Date < DateTime.Now.AddDays(-cutOfDays).Date)
        //        {
        //            isFileExists = true;
        //            break;
        //        }
        //    }

        //    if (isFileExists)
        //    {

        //        using (var memoryStream = new MemoryStream())
        //        {
        //            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        //            {
        //                foreach (string file in fileList)
        //                {
        //                    //get file information
        //                    FileInfo fileInfo = new FileInfo(file);

        //                    if (fileInfo.CreationTime.Date < DateTime.Now.AddDays(-cutOfDays).Date)
        //                    {

        //                        byte[] filebyte = System.IO.File.ReadAllBytes(file);

        //                        var sourceFile = archive.CreateEntry(fileInfo.Name);

        //                        using (var entryStream = sourceFile.Open())
        //                        using (var b = new BinaryWriter(entryStream))
        //                        {
        //                            b.Write(filebyte);
        //                        }

        //                        //Delete file from source path
        //                        File.Delete(file);

        //                        isSuccess = true;
        //                    }
        //                }
        //            }

        //            using (var fileStream = new FileStream(zipfileFullPath, FileMode.Create))
        //            {
        //                memoryStream.Seek(0, SeekOrigin.Begin);
        //                memoryStream.CopyTo(fileStream);
        //            }
        //        }

        //    }
        //    return isSuccess;

        //}

        private bool DeleteFiles(string[] fileList, int cutOfDays, string sourceDirectory, bool isRequiredToPerformSubFolder)
        {

            bool isSuccess = false;

            foreach (string file in fileList)
            {
                FileInfo fileInfo = new FileInfo(file);

                if (fileInfo.CreationTime.Date < DateTime.Now.AddDays(-cutOfDays).Date)
                {
                    File.Delete(file);
                    isSuccess = true;
                }
            }

            //Check purge action required to perform in subfolder
            if (isRequiredToPerformSubFolder)
            {
                foreach (string directory in Directory.GetDirectories(sourceDirectory))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(directory);

                    if (directoryInfo.CreationTime.Date < DateTime.Now.AddDays(-cutOfDays).Date)
                    {
                        //delete sub directories 
                        directoryInfo.Delete(true);
                        isSuccess = true;
                    }
                }
            }

            return isSuccess;
        }

        private bool MoveFiles(string[] fileList, int cutOfDays, string sourceDirectory, string distinationDirectory, bool isRequiredToPerformSubFolder)
        {

            bool isSuccess = false;

            if (string.Empty != distinationDirectory)
            {
                if (!Directory.Exists(distinationDirectory))
                {
                    Directory.CreateDirectory(distinationDirectory);
                }
            }

            foreach (string file in fileList)
            {
                FileInfo fileInfo = new FileInfo(file);

                if (fileInfo.CreationTime.Date < DateTime.Now.AddDays(-cutOfDays).Date)
                {
                    //Is same file name already exist in destinaion path?
                    if (!File.Exists(Path.Combine(distinationDirectory, fileInfo.Name.ToString())))
                    {
                        File.Move(file, Path.Combine(distinationDirectory, fileInfo.Name.ToString()));
                    }
                    else
                    {
                        //Rename file by adding hours,mins and seconds
                        File.Move(file, Path.Combine(distinationDirectory, fileInfo.Name.Replace(fileInfo.Extension, "").ToString()  + "_" + DateTime.Now.ToString("MM-dd-yyyy_hh-mm-ss")) + fileInfo.Extension.ToString());
                    }

                    //If move subdirectories set as true , move folder and their contents to new location.
                    if (isRequiredToPerformSubFolder)
                    {
                        DirectoryInfo directory = new DirectoryInfo(sourceDirectory);
                        //Get directory list
                        DirectoryInfo[] directories = directory.GetDirectories();

                        foreach (DirectoryInfo subDirectory in directories)
                        {

                            if (subDirectory.CreationTime.Date < DateTime.Now.AddDays(-cutOfDays).Date)
                            {
                                string tempSourcepath = Path.Combine(sourceDirectory, subDirectory.Name);
                                string tempDestinationpath = Path.Combine(distinationDirectory, subDirectory.Name);
                                string[] files = Directory.GetFiles(tempSourcepath);

                                //Invoke copy for every sub directory
                                MoveFiles(files, cutOfDays, tempSourcepath, tempDestinationpath, isRequiredToPerformSubFolder);

                                sourceDirectory = tempSourcepath;
                                isSuccess = true;
                            }
                        }
                    }

                    isSuccess = true;
                }
            }

            return isSuccess;
        }

        private bool CopyFiles(string[] fileList, int cutOfDays, string sourceDirectory, string distinationDirectory, bool isRequiredToPerformSubFolder)
        {

            bool isSuccess = false;

            if (string.Empty != distinationDirectory)
            {
                if (!Directory.Exists(distinationDirectory))
                {
                    Directory.CreateDirectory(distinationDirectory);
                }
            }

            foreach (string file in fileList)
            {
                FileInfo fileInfo = new FileInfo(file);

                if (fileInfo.CreationTime.Date < DateTime.Now.AddDays(-cutOfDays).Date)
                {
                    //Is same file name already exist in destinaion path?
                    if (!File.Exists(Path.Combine(distinationDirectory, fileInfo.Name.ToString())))
                    {
                        File.Copy(file, Path.Combine(distinationDirectory, fileInfo.Name.ToString()));
                    }
                    else
                    {
                        //Rename file by adding hours,mins and seconds
                        File.Copy(file, Path.Combine(distinationDirectory, fileInfo.Name.Replace(fileInfo.Extension, "").ToString()  + "_" + DateTime.Now.ToString("MM-dd-yyyy_hh-mm-ss")) + fileInfo.Extension.ToString());
                    }

                    isSuccess = true;
                }
            }

            //If copy subdirectories set as true , copy folder and their contents to new location.
            if (isRequiredToPerformSubFolder)
            {
                DirectoryInfo directory = new DirectoryInfo(sourceDirectory);
                //Get directory list
                DirectoryInfo[] directories = directory.GetDirectories();

                foreach (DirectoryInfo subDirectory in directories)
                {
                    if (subDirectory.CreationTime.Date < DateTime.Now.AddDays(-cutOfDays).Date)
                    {
                        string tempSourcepath = Path.Combine(sourceDirectory, subDirectory.Name);
                        string tempDestinationpath = Path.Combine(distinationDirectory, subDirectory.Name);
                        string[] files = Directory.GetFiles(tempSourcepath);

                        //Invoke copy for every sub directory
                        CopyFiles(files, cutOfDays, tempSourcepath, tempDestinationpath, isRequiredToPerformSubFolder);

                        sourceDirectory = tempSourcepath;
                        isSuccess = true;
                    }
                }
            }

            return isSuccess;
        }

    }
}
