using System;
using System.Collections.Generic;
using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// One required physical role that can be fulfilled by any one of several
    /// complete cable groups.  Unlike a single "any groups" list, several of
    /// these entries can be used together (for example: one DC lamp role and
    /// one AC lamp role), while each entry keeps its own alternatives.
    /// </summary>
    [Serializable]
    public class FreeLabConnectionGroupAlternativeRequirement
    {
        [Tooltip("At least one complete group in this row is required. Add a separate row for each independently required device/cable role.")]
        public List<FreeLabConnectionGroup> alternatives =
            new List<FreeLabConnectionGroup>();

        public bool IsComplete(FreeLabManager lab)
        {
            if (lab == null || alternatives == null || alternatives.Count == 0)
                return false;

            foreach (FreeLabConnectionGroup group in alternatives)
            {
                if (group != null && group.IsComplete(lab))
                    return true;
            }

            return false;
        }
    }
}
