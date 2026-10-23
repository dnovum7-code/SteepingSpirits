using System;
using System.Collections.Generic;
using System.Text;

namespace SteepingSpirits.Processing
{
    /// <summary>The blending table (filled in by backlog item F24).</summary>
    public class BlendStation
    {
        protected readonly TeaWorkshop root;

        public BlendStation(TeaWorkshop root)
        {
            this.root = root;
        }

        public virtual void Fill(StringBuilder text, List<(string, Action)> actions)
        {
            text.Append(ProcessingTexts.BlendSoon);
        }
    }
}
