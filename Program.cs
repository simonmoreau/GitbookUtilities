using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace GitbookUtilities
{
    class Program
    {
        static void Main(string[] args)
        {
            //Find all .md files
            string directoryPath = @"C:\Users\smoreau\Github\BIM-Execution-Plan";

            // Create a DirectoryInfo object representing the specified directory.
            DirectoryInfo dir = new DirectoryInfo(directoryPath);
            // Get the FileInfo objects for every file in the directory.
            FileInfo[] files = dir.GetFiles("*.md", SearchOption.AllDirectories);

            //A file to save the data
            string initialDataPath = @"C:\Users\smoreau\Github\ConsoleApp\GitbookUtilities\initialData.xml";

            if (args[0] == "save")
            {
                //A list of pictures
                List<Picture> pictures = new List<Picture>();

                //Find and save the initial path/hash pair
                foreach (FileInfo file in files)
                {
                    List<FileInfo> imagesFilesInfo = new List<FileInfo>();
                    imagesFilesInfo = FindPicturePathInFile(file.FullName, directoryPath);

                    foreach (FileInfo fInfo in imagesFilesInfo)
                    {
                        pictures.Add(new Picture(fInfo.FullName));
                    }
                }

                //Save the result

                XmlSerializer ser = new XmlSerializer(typeof(List<Picture>));
                TextWriter writer = new StreamWriter(initialDataPath);
                ser.Serialize(writer, pictures);
                writer.Close();
            }
            else
            {
                //A list of pictures
                List<Picture> newPictures = new List<Picture>();

                //Find the new location of all pictures with their hash
                List<string> imageNewPaths = GetFiles(directoryPath, @"\.png|\.jpg", SearchOption.AllDirectories).ToList();
                foreach (string path in imageNewPaths)
                {
                    newPictures.Add(new Picture(path));
                }

                //Fill in the existing list of pictures
                XmlSerializer mySerializer = new XmlSerializer(typeof(List<Picture>));
                // To read the file, create a FileStream.  
                FileStream myFileStream = new FileStream(initialDataPath, FileMode.Open);
                // Call the Deserialize method and cast to the object type. 
                List<Picture> pictures = (List<Picture>)mySerializer.Deserialize(myFileStream);

                foreach (Picture picture in pictures)
                {
                    Picture correspondingNewPicture = newPictures.Where(p => p.Hash == picture.Hash).FirstOrDefault();
                    picture.NewPath = correspondingNewPicture.InitialPath;
                }

                //Replace image paths in the md files
                foreach (FileInfo file in files)
                {
                    ReplacePicturePathInFile(file.FullName, directoryPath, pictures);
                }

            }

        }

        static void ReplacePicturePathInFile(string filePath, string directoryPath, List<Picture> pictures)
        {
            string fullText = File.ReadAllText(filePath);

            foreach (Picture picture in pictures)
            {
                string gitbookInitialPath = picture.InitialGitBookPath(directoryPath);
                string gitbookNewPath = picture.NewGitBookPath(directoryPath);
                fullText = fullText.Replace("(" + gitbookInitialPath + ")", "(" + gitbookNewPath + ")" );
            }

            File.WriteAllText(filePath, fullText);
        }

        static List<FileInfo> FindPicturePathInFile(string filePath, string directoryPath)
        {
            string fullText = File.ReadAllText(filePath);
            //Find images in these files
            //![](/02_Modelisation/02_architecte/images/Coordonnées partagées 06.PNG)   
            //!\[.*?\]\(.*?\)

            List<FileInfo> imagesFilesInfo = new List<FileInfo>();

            Regex regex = new Regex(@"!\[.*?\]\(.*?\)");
            MatchCollection matchCollection = regex.Matches(fullText);
            if (matchCollection.Count != 0)
            {
                foreach (Match match in matchCollection)
                {
                    string imagePath = Regex.Replace(match.Value, @"!\[.*?\]\(", "");
                    imagePath = Regex.Replace(imagePath, @"\)", "");
                    imagePath = imagePath.Replace("/", "\\");
                    string fullImagePath = directoryPath + imagePath;
                    imagesFilesInfo.Add(new FileInfo(fullImagePath));
                }
            }

            return imagesFilesInfo;
        }

        public static IEnumerable<string> GetFiles(string path, string searchPatternExpression, SearchOption searchOption)
        {
            Regex reSearchPattern = new Regex(searchPatternExpression, RegexOptions.IgnoreCase);
            return Directory.EnumerateFiles(path, "*", searchOption)
                            .Where(file =>
                                     reSearchPattern.IsMatch(Path.GetExtension(file)));
        }
    }

    public class Picture
    {

        public string InitialPath { get; set; }
        public string NewPath { get; set; }
        public string Hash { get; set; }

        public Picture(string initialPath)
        {
            InitialPath = initialPath;
            NewPath = "";
            Hash = GetHash();
        }

        public Picture()
        {
            InitialPath = "";
            NewPath = "";
            Hash = "";
        }
        public string NewGitBookPath(string directoryPath)
        {
            return NewPath.Replace(directoryPath, "").Replace("\\", "/");
        }

        public string InitialGitBookPath(string directoryPath)
        {
            return InitialPath.Replace(directoryPath, "").Replace("\\", "/");
        }

        private string GetHash()
        {
            // Initialize a SHA256 hash object.
            SHA256 mySHA256 = SHA256Managed.Create();
            FileInfo fInfo = new FileInfo(this.InitialPath);



            byte[] hashValue;
            // Compute and print the hash values for the file
            // Create a fileStream for the file.
            FileStream fileStream = fInfo.Open(FileMode.Open);
            // Be sure it's positioned to the beginning of the stream.
            fileStream.Position = 0;
            // Compute the hash of the fileStream.
            hashValue = mySHA256.ComputeHash(fileStream);

            // Close the file.
            fileStream.Close();

            return GetByteArray(hashValue);
        }

        // Print the byte array in a readable format.
        public static string GetByteArray(byte[] array)
        {
            string value = "";
            int i;
            for (i = 0; i < array.Length; i++)
            {
                value = value + String.Format("{0:X2}", array[i]);
                if ((i % 4) == 3) value = value + " ";
            }

            return value;
        }
    }
}
