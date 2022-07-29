using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FileData : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI fileName;
    [SerializeField]
    private Button loadButton;
    [SerializeField]
    private TextMeshProUGUI loadButtonText;

    private IFileLoader fileLoader;

    public void SetText(string text)
    {
        fileName.text = text;
    }

    public void SetFileLoader(IFileLoader _fileLoader)
    {
        fileLoader = _fileLoader;
    }

    public void LoadFile()
    {
        var result = fileLoader.LoadFileFromFileName(fileName.text, ResetButton);

        if(result)
        {
            loadButtonText.text = "File Loaded";
        }
        else
        {
            loadButtonText.text = "File Load Failed";
        }
        loadButton.interactable = false;
    }

    public void DeleteFile()
    {
        fileLoader.DeleteFile(fileName.text);
        Destroy(gameObject);
    }

    public void ResetButton()
    {
        loadButtonText.text = "Load File";
        loadButton.interactable = true;
    }
}
