using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// Load example data
var alarmLog = JsonSerializer.Deserialize<IReadOnlyList<AlarmLog>>(File.ReadAllText("alarmlog.json"));
var alarms = JsonSerializer.Deserialize<IReadOnlyList<Alarm>>(File.ReadAllText("alarms.json"));

// Set up endpoints
app.MapGet("/alarms", () =>
{
    var models = new List<AlarmModel>();
    foreach (var alarm in alarms)
    {
        models.Add(
            new AlarmModel
            {
                Id = alarm.AlarmId,
                Station = alarm.Station,
                Number = alarm.AlarmNumber,
                Class = alarm.AlarmClass,
                Text = alarm.AlarmText,
            }
        );
    }

    return models;
})
.WithName("Alarms");

app.MapGet("/alarmLog", () =>
{
    return alarmLog;
})
.WithName("AlarmLog");

app.Run();

// Output models
public class AlarmModel
{
    public int Id { get; set; }

    public string Station { get; set; }

    public int Number { get; set; }

    public string Class { get; set; }

    public string Text { get; set; }
}

// Data models
public record AlarmLog
{
    public int AlarmId { get; init; }

    public LoggedAlarmEvent Event { get; init; }

    public string AckBy { get; init; }

    public DateTime Date { get; init; }
}

public record Alarm
{
    public int AlarmId { get; init; }

    public string Station { get; init; }

    public int AlarmNumber { get; init; }

    public string AlarmClass { get; init; }

    public string AlarmText { get; init; }
}

public enum LoggedAlarmEvent
{
    Off = 0,
    On = 1,
    Acked = 2,
    Blocked = 3,
    UnBlocked = 4,
    AckedLocally = 5,
    Cause = 6,
    Reset = 7,
    PagingSentToUser = 8,
    PagingUserSMSReceived = 9,
    PagingSentSMSToUser = 10,
    PagingSentMailToUser = 11,
    PagingSentPushToUser = 12,
    PagingUserPushReceived = 13,
    PagingUserPushRead = 14,
}