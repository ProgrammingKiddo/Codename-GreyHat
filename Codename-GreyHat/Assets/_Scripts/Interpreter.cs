/*
 * Copyright (c) Borja Fernández
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NetworkSystem;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class Interpreter : MonoBehaviour
{

    #region Variables
    public List<PC> networkPCs = new List<PC>();
    public PC currentPC;
    public string workingDirectory;

    private List<string> response;
    //Map of aliases to the command they represent
    private readonly Dictionary<string, string> aliases = new Dictionary<string, string>()
    {
        {"h",       "help"},
        {"cls",     "clear"},
        {"dir",     "ls"},
        {"lc",      "list-commands"}
    };
    private Dictionary<string, string> visitedPCs = new Dictionary<string, string>();

    private TerminalManager terminal;
    #endregion


    #region UnityMethods
    void Start()
    {
        response = new List<string>();
        terminal = GetComponent<TerminalManager>();
        visitedPCs.Add(currentPC.ipAddress, currentPC.pcName);
    }

    #endregion

    public List<string> Interpret(string userInput)
    {
        // First of all we clear the response list because each interpretation has
        // its own different response
        response.Clear();

        // By default, the whitespace separator is assumed
        string[] args = userInput.Split();
        string command = args[0];
        string param1 = "";
        string param2 = "";
        command = CheckForAlias(command);
        if (args.Length >= 2)
        {
            param1 = args[1];
            if (args.Length >= 3)
            {
                param2 = args[2];
            }
        }

        switch (command)
        {
            case "help":
                if (args.Length > 1)
                {
                    HelpCommand(args[1]);
                }
                else
                {
                    HelpCommand("");
                }
                break;
            case "clear":
                terminal.ClearScreen();
                break;
            case "connect":
                TryConnect(param1, param2);
                break;
            case "pwd":
                PrintWorkingDirectory();
                break;
            case "ls":
                ListFolder(param1);
                break;
            case "cd":
                if (param1.Equals(""))
                {
                    IncompleteCommandSyntax(command);
                }
                else
                {
                    ChangeDirectory(param1);
                }
                break;
            case "list-commands":
                ListCommands(param1);
                break;
            case "cat":
                OpenFile(param1);
                break;
            case "network":
                Network();
                break;
            case "exit":
                ExitConnection();
                break;
            case "send":
                SendFinalMessage(param1);
                break;
            default:
                CommandNotRecognized(command);
                break;
        }
        return response;
    }

    /****************/
    /*  UTILITIES   */
    /****************/

    private string CheckForAlias(string commandAlias)
    {
        string command;
        //If the command is an alias for another command, return its standard name
        if (aliases.TryGetValue(commandAlias, out command) == false)
        {
            //If the command is not an alias, simply return it back
            command = commandAlias;
        }
        return command;
    }
    private void CommandNotRecognized(string command)
    {
        string text = terminal.ColorString(command, Color.red) + ": command not found.";
        response.Add(text);
    }
    private void IncompleteCommandSyntax(string command)
    {
        response.Add(terminal.ColorString("connect", Color.red) + ": missing operand.");
        response.Add("Try 'help " + command + "' for more information.");
    }
    private void IncorrectCommandSyntax(string command, string reason)
    {
        response.Add(terminal.ColorString(command, Color.red) + ": one or more operands are incorrect.");
        if (!reason.Equals(""))
        {
            response.Add(reason);
        }
        response.Add("Try 'help " + command + "' for more information.");
    }
    private void FileNotFound(string filePath)
    {
        response.Add(terminal.ColorString(filePath, Color.red) + ": not found.");
    }
    private bool IsValidIPAddress(string ipAddress)
    {
        bool isValid = false;
        string[] sections = ipAddress.Split('.');

        if (sections.Length == 4)
        {
            int validSections = 0;
            foreach (string section in sections)
            {
                int intSection = Int32.Parse(section);

                if (intSection >= 0 && intSection <= 255)
                {
                    validSections++;
                }
            }
            if (validSections == 4)
            {
                isValid = true;
            }
        }
        return isValid;
    }


    /****************/
    /*  COMMANDS    */
    /****************/

    private void HelpCommand(string command)
    {
        switch (command)
        {
            case "connect":
                // UPDATE
                response.Add(terminal.ColorString("connect ipAddress", Color.green) + ": connect via ssh to a different computer terminal.");
                response.Add("");
                response.Add("Once you have discovered the ip address of another terminal, you can call this command with said address to connect to it.");
                response.Add("Remember that IPv4 addresses follow this format: \"x.x.x.x\", where each section (x) is a number between '0' and '255'.");
                response.Add("After connecting to another terminal, you can either connect back to your machine (" 
                    + terminal.ColorString("19.11.19.98", Color.blue) + ") or simply close the connection.");
                response.Add("to that terminal with " + terminal.ColorString("exit", Color.green) + ".");
                break;
            case "clear":
                response.Add(terminal.ColorString("clear", Color.green) + ": clears the history of previously inputted commands.");
                response.Add("");
                response.Add("Keep in mind that after long usage and/or after a very big history clean you might need to manually");
                response.Add("scroll up to locate the input again.");
                break;
            case "pwd":
                response.Add(terminal.ColorString("pwd", Color.green) + ": prints to the screen the current working directory.");
                response.Add("");
                response.Add("Outputs the absolute filepath (that is, accounting from the root directory) of the current directory you're in right now.");
                break;
            case "ls":
                response.Add(terminal.ColorString("ls [-l]", Color.green) + ": list all files in the current directory.");
                response.Add("\t: list all information from all the files in the current directory. Also displays the number of files.");
                response.Add("");
                response.Add("With the long [-l] option, the file entry format is as follows:");
                response.Add("(d)irectory (last modification date) [last user to modify] filename");
                break;
            case "cd":
                response.Add(terminal.ColorString("cd [path|filename]", Color.green) + ": move to the designated directory.");
                response.Add("\t: you can move to a directory by introducing its absolute path from the file hierarchy.");
                response.Add("\t: alternatively, you can move to a directory contained in your current one. Run "
                    + terminal.ColorString("ls -l", Color.green) + " to check how many are there.");
                response.Add("");
                response.Add("Absolute paths represent the route from the root directory to the designated one, such as: "
                    + terminal.ColorString("/usr/documents", Color.blue) + ".");
                /*response.Add("Relative paths represent the route from the working directory to the designated one, such as "
                    + terminal.ColorString("documents", Color.blue) + ", while the working directory is " + terminal.ColorString("/usr", Color.blue) + " .");*/
                response.Add("Note that you can NOT open a file this way. To open a file, use " + terminal.ColorString("cat", Color.green) + ".");
                break;
            case "list-commands":
                response.Add(terminal.ColorString("list-commands [page]", Color.green) + ": lists all available commands.");
                response.Add("\t: there are three different pages of commands, consecuently named 1, 2, and 3.");
                response.Add("\t: by default, " + terminal.ColorString("list-command 1", Color.green) + " is assumed.");
                break;
            case "cat":
                response.Add(terminal.ColorString("cat [path|filename]", Color.green) + ": opens a file and shows its content on screen.");
                response.Add("\t: you can open a file by introducing its absolute path from the file hierarchy.");
                response.Add("\t: alternatively, you can open a file contained within your current directory. Run "
                    + terminal.ColorString("ls -l", Color.green) + " to check how many are there.");
                response.Add("");
                response.Add("Absolute paths represent the route from the root directory to the designated file, such as: "
                    + terminal.ColorString("/usr/documents/reports.doc", Color.blue) + ".");
                response.Add("Note that you can NOT open a directory this way. To open a directory, use " + terminal.ColorString("cd", Color.green) + ".");
                response.Add("Keep in mind that after long usage and/or after showing a big file you might need to manually");
                response.Add("scroll down to locate the input again.");
                break;
            case "network":
                response.Add(terminal.ColorString("network", Color.green) + ": shows a list of all visited terminals and their ip addresses.");
                response.Add("");
                response.Add("Whenever you visit a terminal, its address and name will be automatically stored for further query with this command.");
                response.Add("Keep in mind, however, that passwords are not stored and you'll have to remember them.");
                break;
            case "exit":
                response.Add(terminal.ColorString("exit", Color.green) + ": closes the connection with the current terminal and returns to your home pc.");
                response.Add("Executing this command while on your own terminal does nothing.");
                break;
            case "send":
                response.Add(terminal.ColorString("send ipAddress", Color.green) + ": send the indicated ip address to the Police as tip of the criminal's identity.");
                response.Add("");
                response.Add("The command is already configured with everything needed to anonymously send the information to the Police.");
                response.Add("Note that, as no connection is being made to that address, failure in format or content won't be checked before sending.");
                response.Add(terminal.ColorString("Use this command only when you've made your decision on who to accuse.", Color.red));
                break;
            default:
                response.Add(terminal.ColorString("help [command]", Color.green) + ": gets general help about the usage of the terminal.");
                response.Add("\t: gets specific help about the indicated command.");
                response.Add("\tIf a command is followed by something else between brackets [], that means you can optionally write something more besides it, but it's not mandatory.");
                response.Add("Like, for example, writing " + terminal.ColorString("help ls", Color.green) + " calls the 'help' command with 'ls' as an option.");
                response.Add("\tIf a command is followed by something else, but is not between brackets, that means you HAVE to write something more besides it, like "
                    + terminal.ColorString("connect ipAddress", Color.green) + ".");
                break;
        }
    }

    private void ListCommands(string parameter)
    {
        if (!parameter.Equals("1") && !parameter.Equals("2") && !parameter.Equals("3"))
        {
            ListCommands("1");
        }
        if (parameter.Equals("1"))
        {
            response.Add(terminal.ColorString("help [command]", Color.green) + ": gets general help about the usage of the terminal.");
            response.Add(terminal.ColorString("list-commands [page]", Color.green) + ": lists all available commands.");
            response.Add(terminal.ColorString("ls [-l]", Color.green) + ": list all files in the current directory.");
            response.Add(terminal.ColorString("pwd", Color.green) + ": prints to screen the current working directory.");
        }
        if (parameter.Equals("2"))
        {
            response.Add(terminal.ColorString("cd [path]", Color.green) + ": moves the working directory to the designated one.");
            response.Add(terminal.ColorString("cat [path|filename]", Color.green) + ": opens a file and shows its content on screen.");
            response.Add(terminal.ColorString("clear", Color.green) + ": clears the history of previously inputted commands.");
            response.Add(terminal.ColorString("send ipAddress", Color.green) + ": send the indicated ip address to the Police as tip of the criminal's identity.");
        }
        if (parameter.Equals("3"))
        {
            response.Add(terminal.ColorString("connect ipAddress", Color.green) + ": connect via ssh to a different computer terminal.");
            response.Add(terminal.ColorString("network", Color.green) + ": shows a list of all visited terminals and their ip addresses.");
            response.Add(terminal.ColorString("exit", Color.green) + ": closes the connection with the current terminal and returns to your home pc.");
        }
    }

    private void PrintWorkingDirectory()
    {
        response.Add(this.workingDirectory);
    }

    private void ListFolder(string parameter)
    {
        List<NetworkSystem.File> filesListed;
        bool longListing = (parameter.Equals("-l")) ? true : false;
        int numberOfDirectories = 0;
        filesListed = currentPC.GetFilesInWorkingDirectory();

        if (parameter.Equals("") || longListing)
        {
            foreach (NetworkSystem.File f in filesListed)
            {
                string fileEntry;
                if (f.isDirectory) numberOfDirectories++;

                if (longListing)
                {
                    fileEntry = f.GetLongFileEntry();
                }
                else
                {
                    fileEntry = f.fileName;
                }
                response.Add(fileEntry);
            }
            if (longListing)
            {
                response.Add("\t" + (filesListed.Count -numberOfDirectories) + " File(s)");
                response.Add("\t" + numberOfDirectories + " Dir(s)");
            }
        }
        else
        {
            IncorrectCommandSyntax("ls", "");
        }
    }

    private void ChangeDirectory(string targetDirectoryPath)
    {
        NetworkSystem.File targetDirectory = currentPC.FindFile(targetDirectoryPath);

        if (targetDirectory == null)
        {
            FileNotFound(targetDirectoryPath);
        }
        else
        {
            if (targetDirectory.isDirectory)
            {
                currentPC.workingDirectory = targetDirectory;
                this.workingDirectory = currentPC.workingDirectory.fileName;
            }
            else
            {
                IncorrectCommandSyntax("cd", terminal.ColorString(currentPC.GetFilePath(targetDirectory), Color.blue) + " is not a directory.");
            }
        }
    }

    private void OpenFile(string targetFilePath)
    {
        NetworkSystem.File targetFile = currentPC.FindFile(targetFilePath);

        if (targetFile == null)
        {
            FileNotFound(targetFilePath);
        }
        else
        {
            if (targetFile.isDirectory == false)
            {
                switch(targetFile.fileContent)
                {
                    case "/":
                        response.Add(terminal.ColorString("Error", Color.red) + ": the file is corrupted beyond recovery.");
                        break;
                    case "":
                        break;
                    default:
                        PrintFile(targetFile.fileContent);
                        break;
                }
            }
            else
            {
                IncorrectCommandSyntax("cat", terminal.ColorString(currentPC.GetFilePath(targetFile), Color.blue) + " is not a file.");
            }
        }
    }

    private void PrintFile(string pathToFileContent)
    {
#if UNITY_WEBGL

        string urlToAsset = Path.Combine(Application.streamingAssetsPath, pathToFileContent);
        StartCoroutine(PrintFileWebGL(urlToAsset));
#else

        StreamReader file = new StreamReader(Path.Combine(Application.streamingAssetsPath, pathToFileContent));

        while (!file.EndOfStream)
        {
            response.Add(file.ReadLine());
        }

        file.Close();
#endif
    }

    System.Collections.IEnumerator PrintFileWebGL(string urlToAsset)
    {
        UnityWebRequest uwr = UnityWebRequest.Get(urlToAsset);
        yield return uwr.SendWebRequest();

        byte[] fileData = uwr.downloadHandler.data;
        string utfString = System.Text.Encoding.UTF8.GetString(fileData, 0, fileData.Length);
        string[] sentences = utfString.Split('\n');
        for (int i = 0; i < sentences.Length; i++)
        {
            response.Add(sentences[i]);
        }

        terminal.AddInterpreterLines(response);
        terminal.RepositionUserInput();
    }

    // Connection Commands
    private void TryConnect(string targetAddress, string password)
    {
        if (targetAddress.Equals(""))
        {
            IncompleteCommandSyntax("connect");
        }
        else
        {
            if (IsValidIPAddress(targetAddress))
            {
                // Unless we're attempting to connect to the machine we're already at
                if (!targetAddress.Equals(currentPC.ipAddress))
                {
                    response.Add("Connecting to " + targetAddress + "...");
                    Connect(targetAddress, password);
                }
            }
            else
            {
                if (targetAddress.Equals("--help"))
                {
                    HelpCommand("connect");
                }
                else
                {
                    IncorrectCommandSyntax("connect", "\nThe operand " + targetAddress + " is not a valid ip address.");
                }
            }
        }
    }

    private void Connect(string targetAddress, string password)
    {
        if (IPExists(targetAddress))
        {
            PC targetPC = GetPCByIP(targetAddress);
            bool hasAccess = false;
            if (targetPC.password.Equals(""))
            {
                hasAccess = true;
            }
            else
            {
                if (targetPC.password.Equals(password))
                {
                    hasAccess = true;
                }
                else
                {
                    response.Add(terminal.ColorString("Connection rejected", Color.red) + ": the terminal at "
                        + terminal.ColorString(targetAddress, Color.blue) + " rejected the connection because the password was incorrect.");
                }
            }

            if (hasAccess)
            {
                // Before changing PCs we restore the working directory
                currentPC.workingDirectory = currentPC.rootDirectory;
                // Then we connect to another PC and set the terminal data accordingly
                currentPC = targetPC;
                currentPC.workingDirectory = currentPC.rootDirectory;
                this.workingDirectory = currentPC.workingDirectory.fileName;
                AddVisitedPC(currentPC);
            }
        }
        else
        {
            response.Add("Connecting...");
            if (targetAddress.Equals("125.23.14.246"))
            {
                response.Add(terminal.ColorString("Error: Server refused connection.", Color.red));
                response.Add("???");
            }
            else
            {
                response.Add(terminal.ColorString("Error: Unreachable node.", Color.red));
            }
        }
    }

    private void Network()
    {
        foreach (KeyValuePair<string, string> connection in visitedPCs)
        {
            response.Add(terminal.ColorString(connection.Key, Color.blue) + " : " + connection.Value);
        }
    }

    private void ExitConnection()
    {
        TryConnect(networkPCs[0].ipAddress, "");
    }

    private void SendFinalMessage(string address)
    {
        if (address.Equals("") == false)
        {
            // PC de ???
            if (address.Equals("125.23.14.246"))
            {
                PlayerPrefs.SetInt("Final", 1);
            }
            else
            {
                // PC de Helen
                if (address.Equals("168.80.136.136"))
                {
                    PlayerPrefs.SetInt("Final", 0);
                }
                else
                {
                    // Cualquier otra cosa
                    PlayerPrefs.SetInt("Final", -1);
                }
            }
            SceneManager.LoadScene("FinalScene");
        }
        else
        {
            IncompleteCommandSyntax("send");
        }
    }

    /****************************/
    /*  INTERNAL CHECK COMMANDS */
    /****************************/

    public bool IPExists(string ipAddress)
    {
        bool ipExists = false;
        int i = 0;
        while (i < networkPCs.Count && ipExists == false)
        {
            if (networkPCs[i].ipAddress.Equals(ipAddress))
            {
                ipExists = true;
            }
            i++;
        }
        return ipExists;
    }

    public PC GetPCByIP(string address)
    {
        PC lookedUpPC = null;
        foreach (PC node in networkPCs)
        {
            if (node.ipAddress.Equals(address))
            {
                lookedUpPC = node;
            }
        }
        return lookedUpPC;
    }

    public void AddVisitedPC(PC visitedPC)
    {
        if (!visitedPCs.ContainsKey(visitedPC.ipAddress))
        {
            visitedPCs.Add(visitedPC.ipAddress, visitedPC.pcName);
        }
    }
}
