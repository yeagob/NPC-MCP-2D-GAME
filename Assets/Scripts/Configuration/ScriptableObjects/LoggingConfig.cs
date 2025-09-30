using UnityEngine;

[CreateAssetMenu(fileName = "LoggingConfig", menuName = "LLM/Logging Configuration")]
public class LoggingConfig : ScriptableObject
{
    [SerializeField] private LogLevel minimumLogLevel;
    [SerializeField] private bool enableAgentExecution;
    [SerializeField] private bool enableToolCalls;
    [SerializeField] private bool enableMessageReception;
    [SerializeField] private bool enablePromptConstruction;
    [SerializeField] private bool enableContextUpdates;
    [SerializeField] private bool enablePerformanceMetrics;

    public LogLevel MinimumLogLevel => minimumLogLevel;
    public bool EnableAgentExecution => enableAgentExecution;
    public bool EnableToolCalls => enableToolCalls;
    public bool EnableMessageReception => enableMessageReception;
    public bool EnablePromptConstruction => enablePromptConstruction;
    public bool EnableContextUpdates => enableContextUpdates;
    public bool EnablePerformanceMetrics => enablePerformanceMetrics;
}
