/*
 * Copyright (c) Borja Fernández
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkSystem
{
    [CreateAssetMenu(fileName = "New PC")]
    public class PC : ScriptableObject
    {

        #region Variables
        public string ipAddress;
        public string ipMask;
        public string pcName;
        public string password;
        public File rootDirectory;
        public File workingDirectory;
        #endregion



        public File FindFile(string filePath)
        {
            if (filePath.StartsWith("/"))
            {
                return FindFileAbsolute(filePath);
            }
            else
            {
                if (filePath.StartsWith("./"))
                {
                    filePath.TrimStart(new[] { '.', '/' });
                }
                return FindFileRelative(filePath);
            }
        }

        public File FindFileAbsolute(string filePath)
        {
            File searchedFile = null;

            if (filePath.Equals("/"))
            {
                searchedFile = rootDirectory;
            }
            else
            {
                File auxFile = rootDirectory;
                string[] directories = filePath.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
                bool fileFound = true;
                int i = 0;

                while (i < directories.Length && fileFound == true)
                {
                    fileFound = auxFile.TryGetFile(directories[i], out searchedFile);
                    auxFile = searchedFile;
                    i++;
                }
                if (fileFound == false)
                    searchedFile = null;
            }

            return searchedFile;
        }

        public File FindFileRelative(string filePath)
        {
            if (workingDirectory.Equals(rootDirectory))
            {
                return FindFileAbsolute("/" + filePath);
            }
            else
            {
                return FindFileAbsolute(GetFilePath(workingDirectory) + "/" + filePath);
            }
        }

        public string GetFilePath(File file)
        {
            string path = "";
            File auxFile = file;

            while (auxFile != rootDirectory)
            {
                path = "/" + auxFile.fileName + path;
                auxFile = auxFile.parent;
            }
            if (path.Equals(""))
            {
                path = "/";
            }

            return path;
        }

        public List<File> GetFilesInWorkingDirectory()
        {
            List<File> children = new List<File>();

            foreach(File f in workingDirectory.childs)
            {
                children.Add(f);
            }

            return children;
        }
    }
}