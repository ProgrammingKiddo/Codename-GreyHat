/*
 * Copyright (c) Borja Fernández
 * Based on the terminal system by Fouriersoft
 * www.youtube.com/channel/UCgVQGgRHtY9Vef_a254x3Lw
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TerminalManager : MonoBehaviour
{

    #region Variables
    public GameObject directoryLine;
    public GameObject responseLine;
    public InputField terminalInput;
    public GameObject userInputLine;
    public ScrollRect terminalScroll;
    public GameObject messageList;
    public GameObject downArrow;

    private Interpreter interpreter;
    private bool isInputOutOfView = false;
    #endregion


    #region UnityMethods

    void Start()
    {
        interpreter = GetComponent<Interpreter>();
        //Start execution with the focus already set on the input field
        terminalInput.ActivateInputField();
        terminalInput.Select();
        RepositionUserInput();
    }

    void Update()
    {
        if (terminalInput.isFocused == false)
        {
            terminalInput.Select();
        }
        // Command history "cycling", only the last command introduced
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            HorizontalLayoutGroup[] childrenGroups = GetComponentsInChildren<HorizontalLayoutGroup>();

            for (int i = childrenGroups.Length-1; i >= 0; i--)
            {
                if (childrenGroups[i].GetComponentsInChildren<LayoutElement>().Length == 2)
                {
                    terminalInput.text = childrenGroups[i].GetComponentsInChildren<Text>()[1].text;
                    terminalInput.MoveTextEnd(false);
                    return;
                }
            }
        }
        ShowDownArrow();
    }

    #endregion

    private void OnGUI()
    {
        if (terminalInput.isFocused && PressedEnter())
        {
            if (terminalInput.text != "")
            {
                //Store the input for processing and clear the input field
                string userInput = terminalInput.text;
                ClearInputField();

                //Instantiate a new line with directory prefix to show the introduced command
                AddDirectoryLine(userInput);

                //Add the interpretation lines
                AddInterpreterLines(interpreter.Interpret(userInput));
                //ScrollToBottom(lines);
            }
            // Whether the user entered some text or not, we maintain focus on the input field
            RepositionUserInput();
        }
    }

    /**********************************/
    /*  CONTENT GENERATION METHODS    */
    /**********************************/

    private void AddDirectoryLine(string userInput)
    {
        //Resizing the command line container
        Vector2 messageListSize = messageList.GetComponent<RectTransform>().sizeDelta;
        messageList.GetComponent<RectTransform>().sizeDelta = new Vector2(messageListSize.x, messageListSize.y + 35.0f);

        //Instantiate a new directory line
        GameObject msg = Instantiate(directoryLine, messageList.transform);
        //Set this child index to the last one
        msg.transform.SetSiblingIndex(messageList.transform.childCount - 1);

        msg.GetComponentsInChildren<Text>()[1].text = userInput;
        msg.GetComponentsInChildren<Text>()[0].text = interpreter.currentPC.pcName + ":" + interpreter.workingDirectory + "$";
    }

    public void AddInterpreterLines(List<string> interpretation)
    {
        if (interpretation.Count > 0)
        {
            foreach(string s in interpretation)
            {
                //Instantiate the response line
                GameObject response = Instantiate(responseLine, messageList.transform);

                response.transform.SetAsLastSibling();
                Vector2 responseListSize = messageList.GetComponent<RectTransform>().sizeDelta;
                messageList.GetComponent<RectTransform>().sizeDelta = new Vector2(responseListSize.x, responseListSize.y + 35.0f);

                response.GetComponentInChildren<Text>().text = s;
            }
        }
    }

    /****************************/
    /*  PUBLIC UTILITY METHODS  */
    /****************************/

    public void ClearScreen()
    {
        HorizontalLayoutGroup[] childrenGroups = GetComponentsInChildren<HorizontalLayoutGroup>();
        foreach (HorizontalLayoutGroup childrenGroup in childrenGroups)
        {
            if (childrenGroup.gameObject.CompareTag("text"))
            {
                Destroy(childrenGroup.gameObject);
            }
        }
        RepositionUserInput();
        //ScrollToTop();
    }
    public void TrimHistory(int linesToTrim)
    {
        HorizontalLayoutGroup[] childrenGroups = GetComponentsInChildren<HorizontalLayoutGroup>();
        foreach (HorizontalLayoutGroup childrenGroup in childrenGroups)
        {
            if (linesToTrim > 0)
            {
                Destroy(childrenGroup.gameObject);
                linesToTrim--;
            }
        }
    }
    public void LoadTitle(string path, string color, int spacing)
    {
        StreamReader file = new StreamReader(Path.Combine(Application.streamingAssetsPath, path));

        for (int i = 0; i < spacing; i++)
        {
            
        }
    }
    public string ColorString(string stringToColor, Color color)
    {
        string leftTag = "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">";
        string rightTag = "</color>";
        return leftTag + stringToColor + rightTag;
    }

    /****************************/
    /*  PRIVATE UTILITY METHODS */
    /****************************/

    private bool PressedEnter()
    {
        return (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter));
    }

    private void ScrollToBottom(int lines)
    {
        if (lines > 8)
        {
            terminalScroll.velocity = new Vector2(0, 650);
        }
        else
        {
            terminalScroll.verticalNormalizedPosition = 0;
        }
    }

    private void ScrollToTop()
    {
        terminalScroll.velocity = new Vector2(0, -650);
    }

    private void ShowDownArrow()
    {
        RectTransform scrollMaskRect = terminalScroll.GetComponent<RectTransform>();
        RectTransform inputRectTransform = terminalInput.GetComponent<RectTransform>();

        // If the input field is visible (it's on screen)
        if (Camera.main.WorldToScreenPoint(terminalInput.transform.position).y > 0)
        {
            isInputOutOfView = false;
            if (downArrow.activeInHierarchy == true)
            {
                downArrow.SetActive(false);
            }
        }
        // If the input field is not visible (it's off screen)
        else
        {
            isInputOutOfView = true;
            if (downArrow.activeInHierarchy == false)
            {
                downArrow.SetActive(true);
            }
        }

    }

    private void ClearInputField()
    {
        terminalInput.text = "";
    }


    public void RepositionUserInput()
    {
        //Move the user input line to the bottom
        userInputLine.transform.SetAsLastSibling();
        userInputLine.GetComponentsInChildren<Text>()[0].text = interpreter.currentPC.pcName + ":" + interpreter.workingDirectory + "$";
        //Put the focus again in the input line
        terminalInput.ActivateInputField();
        terminalInput.Select();
    }

}
