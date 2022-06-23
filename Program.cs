using System.Text.Json;
using System.Collections.Specialized;
using System.Linq;

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

    Console.WriteLine(models.GetType());
    return models;
})
.WithName("Alarms");

app.MapGet("/alarmLog", () =>
{
    return alarmLog;
})
.WithName("AlarmLog");

/* get logs, get alarms, create new list of station+alarmtext+activations based on alarmID,
 sorted by activation count. unsure if the classes (ABC/E) should be differentiated or if, e.g.
 events (E) should be ignored. it would be easy to add conditions for this to the logic */
app.MapGet("/act_per_alarm", () =>
{
    /* made a new object to contain the three variables: station name, alarm text, activation count
     it also includes AlarmID to make it easier to filter results/build a list below,
     even though the intended endpoint will not use/display the AlarmID */
    List<ActivationReturnObject> activations = new List<ActivationReturnObject>();

    foreach (var item in alarmLog)
    {
        try {
        var alarmID = alarms.FirstOrDefault(a => a.AlarmId == item.AlarmId).AlarmId;
        var stationName = alarms.FirstOrDefault(a => a.AlarmId == item.AlarmId).Station;
        var alarmText = alarms.FirstOrDefault(a => a.AlarmId == item.AlarmId).AlarmText;
        // check if the alarm exists in the list and, if not, add it
        if (!activations.Any(x => x.AlarmID == alarmID))
        {
            activations.Add(new ActivationReturnObject
            {
                AlarmID = alarmID,
                Station = stationName,
                Label = alarmText,
                Count = 1
            });
        }
        // otherwise, just iterate the count
        else
        {
            activations.FirstOrDefault(x => x.AlarmID == alarmID).Count++;
        }
        }
        catch (NullReferenceException ex)
        {
            Console.WriteLine("NullReferenceException: Unable to match activation to an existing station");
            Console.WriteLine(ex.StackTrace);
        }
    }
    return activations.OrderByDescending(x => x.Count);
})
.WithName("Act_per_alarm");



app.MapGet("/act_per_station", () =>
{
    // if we have a large number of stations, a hashtable might be in order instead
    // but with the current sample data, ListDictionary seems fine
    SortedList<string, int> stationList = new SortedList<string, int>();
    foreach (var item in alarmLog)
    {
        // use AlarmID to fetch the station name from the alarms list
        try
        {
            var stationName = alarms.FirstOrDefault(a => a.AlarmId == item.AlarmId).Station;
            if (stationList.ContainsKey(stationName))
            {
                stationList[stationName]++;
                Console.WriteLine(stationList);
            }
            else
            {
                stationList.Add(stationName, 1);
                Console.WriteLine(stationList);
            }
        }
        // should the function check for null objects as part of the if-else bit?
        // or does that add nothing beyond what the try-catch does?
        catch (NullReferenceException ex)
        {
            Console.WriteLine("NullReferenceException: Unable to match activation to an existing station");
            Console.WriteLine(ex.StackTrace);
        }
    }
    return stationList.OrderByDescending(x => x.Value);
})
.WithName("Act_per_station");


app.MapGet("/status", () =>
{
    var statusReport = new StatusReturnObject();
    List<int> activeAlarms = new List<int>();
    foreach (var item in alarmLog)
    {
        int eventEnum = (int)item.Event;
        /* check if alarm is activated and add it to the active list (if it's not on there already)
           also, add an alarm activation to the tally. we assume that an alarm is turned off unless
           otherwise indicated */
        if (eventEnum == 1 && !activeAlarms.Contains(item.AlarmId))
        {
            statusReport.ActiveAlarms++;
            statusReport.Activations++;
            activeAlarms.Add(item.AlarmId);
        }
        // remove alarm if curently on and reported as turned off
        else if (eventEnum == 0 && activeAlarms.Contains(item.AlarmId))
        {
            statusReport.ActiveAlarms--;
            activeAlarms.Remove(item.AlarmId);
        }
        /* add pagings to the tally. should this include all paging events or just the initial
           sent-to-user one? if the latter, swap condition for:
           (eventEnum == 8) */
        else if (8 <= eventEnum && eventEnum <= 14)
        {
            statusReport.Pagings++;
        }
    }
    return statusReport;
})
.WithName("Status");


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

public class StatusReturnObject
{
    public int ActiveAlarms { get; set; }

    public int Activations { get; set; }

    public int Pagings { get; set; }
}

public class ActivationReturnObject
{
    public int AlarmID { get; set; }

    public string Station { get; set; }

    public string Label { get; set; }

    public int Count { get; set; }

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