using System;
using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Describes one authored or runtime design-system change.</summary>
    public readonly struct LoogaDesignSystemChange
    {
        /// <summary>Creates a change record for one asset revision.</summary>
        public LoogaDesignSystemChange(UnityEngine.Object source, int revision)
        {
            Source = source;
            Revision = revision;
        }

        /// <summary>Gets the asset that changed.</summary>
        public UnityEngine.Object Source { get; }

        /// <summary>Gets the asset revision after the change.</summary>
        public int Revision { get; }
    }

    /// <summary>Publishes infrequent design changes without a frame update loop.</summary>
    public static class LoogaDesignSystemEvents
    {
        /// <summary>Occurs after any loaded Looga design asset changes.</summary>
        public static event Action<LoogaDesignSystemChange> Changed;

        internal static void Publish(UnityEngine.Object source, int revision)
        {
            Changed?.Invoke(new LoogaDesignSystemChange(source, revision));
        }
    }
}
