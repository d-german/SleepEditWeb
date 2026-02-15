namespace SleepEditWeb.Application.Protocol;

public static class ProtocolResultMapping
{
    public static string ToUserSafeMessage(ProtocolResult result)
    {
        if (result.IsSuccess)
        {
            return string.Empty;
        }

        return result.ErrorCode switch
        {
            "parent_not_found" => "The selected parent node was not found.",
            "node_not_found" => "The selected node was not found.",
            "invalid_move" => "The requested move is not valid for this protocol node.",
            "invalid_subtext" => "SubText value is required.",
            _ => "Unable to apply protocol change."
        };
    }
}
