using TMPro;
using UnityEngine;

public class UniversalLogUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _textLog;
    
    public static UniversalLogUI Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void Log(string message)
    {
        _textLog.text += message + "\n";
    }
}
