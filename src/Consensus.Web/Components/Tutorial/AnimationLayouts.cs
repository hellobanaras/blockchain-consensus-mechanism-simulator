using Consensus.Web.Components.Tutorial;
using AC = Consensus.Web.Components.Tutorial.ConsensusAnimation;

namespace Consensus.Web.Components.Tutorial;

/// <summary>
/// Reusable positioning helpers for the protocol-lifecycle animations.
/// The SVG viewBox is 800×480; helpers place actors on a ring around the
/// canvas centre so the message-passing lines stay readable.
/// </summary>
public static class AnimationLayouts
{
    // Canvas centre (matches ConsensusAnimation.razor viewBox).
    private const double Cx = 400, Cy = 240;
    private const double Radius = 175;

    private static readonly string[] Palette =
    {
        "#1f7ad8", "#16a085", "#9b59b6", "#e67e22", "#e74c3c",
        "#2c3e50", "#27ae60", "#8e44ad", "#d35400", "#c0392b",
        "#34495e", "#0e91a3", "#7d3c98", "#a04000", "#922b21"
    };

    /// <summary>
    /// Arrange `count` actors evenly around a circle.
    /// </summary>
    public static List<AC.Actor> Ring(
        int count,
        string idPrefix = "n",
        string labelPrefix = "Node",
        string icon = "",
        double radius = Radius)
    {
        var actors = new List<AC.Actor>(count);
        // Start at the top (-π/2) so node 1 sits at 12 o'clock.
        for (int i = 0; i < count; i++)
        {
            var angle = -Math.PI / 2 + i * 2 * Math.PI / count;
            var x = Cx + radius * Math.Cos(angle);
            var y = Cy + radius * Math.Sin(angle);
            actors.Add(new AC.Actor(
                Id: $"{idPrefix}{i + 1}",
                Label: $"{labelPrefix} {i + 1}",
                X: Math.Round(x, 1),
                Y: Math.Round(y, 1),
                Color: Palette[i % Palette.Length],
                Icon: string.IsNullOrEmpty(icon) ? null : icon));
        }
        return actors;
    }

    /// <summary>
    /// Same as Ring but places one actor (e.g. a client) at the centre and
    /// arranges the rest in a circle around it.
    /// </summary>
    public static List<AC.Actor> RingWithCentre(
        int peripheralCount,
        string centreId,
        string centreLabel,
        string centreColor,
        string centreIcon,
        string peripheralIdPrefix = "n",
        string peripheralLabelPrefix = "Node",
        string peripheralIcon = "")
    {
        var actors = new List<AC.Actor>
        {
            new(centreId, centreLabel, Cx, Cy, centreColor, centreIcon, Radius: 36)
        };
        actors.AddRange(Ring(peripheralCount, peripheralIdPrefix,
            peripheralLabelPrefix, peripheralIcon));
        return actors;
    }

    /// <summary>
    /// Helper to broadcast a message from one source to many targets, with
    /// a small stagger so the message-bubble fan-out is visible rather than
    /// piling on top of itself.
    /// </summary>
    public static IEnumerable<AC.Message> Broadcast(
        string fromId,
        IEnumerable<string> toIds,
        string color,
        string? label = null,
        int durationMs = 900,
        int staggerMs = 60)
    {
        int i = 0;
        foreach (var to in toIds)
        {
            yield return new AC.Message(fromId, to, color, label, DelayMs: i * staggerMs, DurationMs: durationMs);
            i++;
        }
    }

    /// <summary>
    /// All-to-all message blast (PBFT Prepare / Commit phases). Skips
    /// self-edges. Color and label apply to every message; stagger keeps
    /// the visual readable.
    /// </summary>
    public static IEnumerable<AC.Message> AllToAll(
        IEnumerable<string> participantIds,
        string color,
        string? label = null,
        int durationMs = 800,
        int staggerMs = 30)
    {
        var ids = participantIds.ToList();
        int i = 0;
        foreach (var from in ids)
        {
            foreach (var to in ids)
            {
                if (from == to) continue;
                yield return new AC.Message(from, to, color, label, DelayMs: i * staggerMs, DurationMs: durationMs);
                i++;
            }
        }
    }
}
