using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FileLoadData : MonoBehaviour
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
        var result = fileLoader.LoadFileFromFileName(fileName.text);

        if(result)
        {
            loadButtonText.text = "File Load Failed";
        }
        else
        {
            loadButtonText.text = "File Loaded";
        }
        loadButton.interactable = false;
    }

    public void DeleteFile()
    {
        fileLoader.DeleteFile(fileName.text);
        Destroy(gameObject);
    }
}
