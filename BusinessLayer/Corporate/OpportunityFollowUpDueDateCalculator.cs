namespace BusinessLayer.Corporate;

/// <summary>
/// Default due dates for required opportunity follow-ups (end of day UTC).
/// </summary>
public static class OpportunityFollowUpDueDateCalculator
{
    public static DateTime GetDueAt(string stageKey)
    {
        var daysFromToday = stageKey switch
        {
            "first_contact" => 0,
            "follow_up" => 3,
            "proposal_sent" => 2,
            "proposal_follow_up" => 3,
            "negotiation" => 5,
            "formalization" => 7,
            _ => 3
        };

        var dueDate = DateTime.UtcNow.Date.AddDays(daysFromToday);
        return dueDate.AddDays(1).AddSeconds(-1);
    }
}
