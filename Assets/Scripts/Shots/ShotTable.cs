using System;
using UnityEngine;

/// <summary>
/// Maps (category + contact situation) to a shot. Rules are checked top to bottom and the first
/// match wins, so put specific rules (e.g. smash returns) above general ones. Changing which
/// shot a button plays is an asset edit, not a code change.
/// </summary>
[CreateAssetMenu(fileName = "ShotTable", menuName = "Badminton/Shot Table")]
public class ShotTable : ScriptableObject
{
    [Flags]
    public enum ZoneMask { None = 0, Front = 1, Mid = 2, Back = 4, Any = Front | Mid | Back }

    public enum SmashReturnRule { Either = 0, OnlyWhenReturningSmash = 1, NeverWhenReturningSmash = 2 }

    [Serializable]
    public class Rule
    {
        [Tooltip("Just a label so the list is readable in the Inspector.")]
        [SerializeField] private string note;
        [SerializeField] private ShotCategory category = ShotCategory.Defensive;
        [SerializeField] private ZoneMask zones = ZoneMask.Any;
        [Tooltip("Contact height range in metres: min inclusive, max exclusive.")]
        [SerializeField] private float minContactHeight = 0f;
        [SerializeField] private float maxContactHeight = 99f;
        [SerializeField] private SmashReturnRule smashReturn = SmashReturnRule.Either;
        [SerializeField] private ShotData shot;

        public ShotData Shot => shot;

        public bool Matches(ShotCategory requested, in ShotContext context)
        {
            if (requested != category) return false;
            if ((zones & (ZoneMask)(1 << (int)context.Zone)) == 0) return false;
            if (context.ContactHeight < minContactHeight || context.ContactHeight >= maxContactHeight) return false;
            if (smashReturn == SmashReturnRule.OnlyWhenReturningSmash && !context.ReturningSmash) return false;
            if (smashReturn == SmashReturnRule.NeverWhenReturningSmash && context.ReturningSmash) return false;
            return true;
        }
    }

    [Header("Court zones (distance from the net on the hitter's side)")]
    [Tooltip("Closer than this = front court. Real short service line is at 1.98 m.")]
    [SerializeField] private float frontZoneMaxDepth = 2.2f;
    [Tooltip("Further than this = back court. Between the two = mid court.")]
    [SerializeField] private float backZoneMinDepth = 4.5f;

    [SerializeField] private Rule[] rules = Array.Empty<Rule>();

    public CourtZone ZoneFor(float depthFromNet)
    {
        if (depthFromNet < frontZoneMaxDepth) return CourtZone.Front;
        return depthFromNet >= backZoneMinDepth ? CourtZone.Back : CourtZone.Mid;
    }

    /// <returns>The first matching rule's shot, or null if no rule matches.</returns>
    public ShotData Select(ShotCategory category, in ShotContext context)
    {
        foreach (Rule rule in rules)
        {
            if (rule != null && rule.Matches(category, context)) return rule.Shot;
        }
        return null;
    }
}
