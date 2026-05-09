// GENERATED FILE - DO NOT MODIFY MANUALLY
namespace Telegram.Bot.Types;

/// <summary>At most <b>one</b> of the optional fields can be present in any given object.</summary>
public partial class PollMedia
{
    /// <summary><em>Optional</em>. Media is an animation, information about the animation</summary>
    public Animation? Animation { get; set; }

    /// <summary><em>Optional</em>. Media is an audio file, information about the file; currently, can't be received in a poll option</summary>
    public Audio? Audio { get; set; }

    /// <summary><em>Optional</em>. Media is a general file, information about the file; currently, can't be received in a poll option</summary>
    public Document? Document { get; set; }

    /// <summary><em>Optional</em>. Media is a live photo, information about the live photo</summary>
    [JsonPropertyName("live_photo")]
    public LivePhoto? LivePhoto { get; set; }

    /// <summary><em>Optional</em>. Media is a shared location, information about the location</summary>
    public Location? Location { get; set; }

    /// <summary><em>Optional</em>. Media is a photo, available sizes of the photo</summary>
    public PhotoSize[]? Photo { get; set; }

    /// <summary><em>Optional</em>. Media is a sticker, information about the sticker; currently, for poll options only</summary>
    public Sticker? Sticker { get; set; }

    /// <summary><em>Optional</em>. Media is a venue, information about the venue</summary>
    public Venue? Venue { get; set; }

    /// <summary><em>Optional</em>. Media is a video, information about the video</summary>
    public Video? Video { get; set; }
}
