/*
 * Copyright (c) Borja Fernández
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkSystem
{
    [CreateAssetMenu(fileName = "New File")]
    public class File : ScriptableObject
    {

        #region Variables
        public bool isDirectory;
        public string fileName;
        public string lastModificationDate;
        public string lastModificationUser;
        public string fileContent;
        public List<File> childs = new List<File>();
        public File parent;
        #endregion

        public string GetLongFileEntry()
        {
            string fileEntry;
            if (isDirectory)
            {
                fileEntry = "d ";
            }
            else
            {
                fileEntry = "- ";
            }
            fileEntry += "(" + lastModificationDate + ")" + " [" + lastModificationUser + "] " + fileName;

            return fileEntry;
        }
        public bool TryGetFile(string filename, out File file)
        {
            bool fileFound = false;
            file = null;
            foreach (File f in childs)
            {
                if (f.fileName.Equals(filename))
                {
                    file = f;
                    fileFound = true;
                }
            }
            return fileFound;
        }
    }
}