using System;
using System.Collections.Generic;

namespace GameLabo
{
    /// <summary>
    /// Eventsのベースクラス
    /// </summary>
    public class BaseEvents : IDisposable
    {
        public Dictionary<ushort, int> EVENTS_IDS;

        public BaseEvents()
        {
            EVENTS_IDS = new Dictionary<ushort, int>();
        }

        public virtual void Dispose()
        {
            EVENTS_IDS = null;
        }

        public virtual void Update()
        {

        }

        public virtual void Show()
        {

        }
    }
}
