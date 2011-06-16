﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AxeSoftware.Quest.Scripts;

namespace AxeSoftware.Quest
{
    internal class TimerRunner
    {
        private WorldModel m_worldModel;
        private Element m_gameElement;

        public TimerRunner(WorldModel worldModel)
        {
            m_worldModel = worldModel;
            foreach (Element timer in m_worldModel.Elements.GetElements(ElementType.Timer))
            {
                timer.Fields[FieldDefinitions.Trigger] = timer.Fields[FieldDefinitions.Interval];
            }
        }

        private Element GameElement
        {
            get
            {
                if (m_gameElement == null)
                {
                    m_gameElement = m_worldModel.Elements.Get("game");
                }
                return m_gameElement;
            }
        }

        private int TimeElapsed
        {
            get { return GameElement.Fields[FieldDefinitions.TimeElapsed]; }
            set { GameElement.Fields[FieldDefinitions.TimeElapsed] = value; }
        }

        public IEnumerable<IScript> TickAndGetScripts(int elapsedTime)
        {
            TimeElapsed += elapsedTime;
            System.Diagnostics.Debug.Print("Time: {0}", TimeElapsed);

            List<IScript> scripts = new List<IScript>();

            foreach (Element timer in m_worldModel.Elements.GetElements(ElementType.Timer))
            {
                System.Diagnostics.Debug.Print("{0}: Next trigger at {1}", timer.Name, timer.Fields[FieldDefinitions.Trigger]);
                if (TimeElapsed >= timer.Fields[FieldDefinitions.Trigger])
                {
                    System.Diagnostics.Debug.Print("     - TRIGGER");
                    scripts.Add(timer.Fields[FieldDefinitions.Script]);
                    timer.Fields[FieldDefinitions.Trigger] += timer.Fields[FieldDefinitions.Interval];
                }
            }

            return scripts;
        }

        public int GetTimeUntilNextTimerRuns()
        {
            // TO DO: If no timers enabled, return 0

            int nextTrigger = TimeElapsed + 60;
            foreach (Element timer in m_worldModel.Elements.GetElements(ElementType.Timer))
            {
                if (timer.Fields[FieldDefinitions.Trigger] < nextTrigger)
                {
                    nextTrigger = timer.Fields[FieldDefinitions.Trigger];
                }
            }

            return (nextTrigger - TimeElapsed);
        }
    }
}