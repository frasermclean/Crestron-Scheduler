using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
//using Crestron.SimplSharp.Scheduler;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;

using UI = AVPlus.Utils.UI.UserInterfaceHelper;

namespace FM.Utilities
{
    public class Scheduler
    {
        #region constants
        // auto start buttons
        const uint BTN_START_MON = 1;
        const uint BTN_START_TUE = 2;
        const uint BTN_START_WED = 3;
        const uint BTN_START_THU = 4;
        const uint BTN_START_FRI = 5;
        const uint BTN_START_SAT = 6;
        const uint BTN_START_SUN = 7;
        const uint BTN_START_HOUR_INC = 8;
        const uint BTN_START_HOUR_DEC = 9;
        const uint BTN_START_MINUTE_INC = 10;
        const uint BTN_START_MINUTE_DEC = 11;

        // auto stop buttons
        const uint BTN_STOP_MON = 12;
        const uint BTN_STOP_TUE = 13;
        const uint BTN_STOP_WED = 14;
        const uint BTN_STOP_THU = 15;
        const uint BTN_STOP_FRI = 16;
        const uint BTN_STOP_SAT = 17;
        const uint BTN_STOP_SUN = 18;
        const uint BTN_STOP_HOUR_INC = 19;
        const uint BTN_STOP_HOUR_DEC = 20;
        const uint BTN_STOP_MINUTE_INC = 21;
        const uint BTN_STOP_MINUTE_DEC = 22;
  
        // text files
        const uint TEXT_START = 1;
        const uint TEXT_STOP = 2;

        // limits
        const uint LIMIT_HOUR_MAX = 23;
        const uint LIMIT_MINUTE_MAX = 59;
        const uint LIMIT_DAYS = 7;
        #endregion

        #region class variables

        // debugging
        public bool debug_enable { get; set; }

        BasicTriList panel;
        uint buttonOffset;

        bool[] daysStart;
        bool[] daysStop;
        uint hourStart, hourStop;
        uint minuteStart, minuteStop;

        #endregion

        public Scheduler(BasicTriList panel, uint buttonOffset)
        {
            this.panel = panel;
            this.buttonOffset = buttonOffset;

            daysStart = new bool[LIMIT_DAYS];
            daysStop = new bool[LIMIT_DAYS];

            panel.SigChange += new SigEventHandler(panel_SigChange);
            panel.OnlineStatusChange += new OnlineStatusChangeEventHandler(panel_OnlineStatusChange);
        }

        /// <summary>
        /// Outputs debugging information to the Console (if enabled)
        /// </summary>
        /// <param name="message">Message to print to console.</param>
        void Debug(string message)
        {
            if (debug_enable)
            {
                string debugMessage = message.Trim();
                CrestronConsole.PrintLine("[Scheduler] " + debugMessage);
            }
        }

        /// <summary>
        /// Updates panel feedback based on class variables
        /// </summary>
        void PanelFeedback()
        {
            uint button;

            for (int i = 0; i < LIMIT_DAYS; i++)
            {
                // start days
                button = (uint)(BTN_START_MON + i + buttonOffset);
                UI.SetDigitalJoin(panel, button, daysStart[i]);

                // stop days
                button = (uint)(BTN_STOP_MON + i + buttonOffset);
                UI.SetDigitalJoin(panel, button, daysStop[i]);
            }

            // start time text
            string timeStart = String.Format("{0}:{1:D2}", hourStart, minuteStart);
            button = TEXT_START + buttonOffset;
            UI.SetSerialJoin(panel, button, timeStart);
            Debug("Setting " + button + " to " + timeStart);

            // stop time text
            string timeStop = String.Format("{0}:{1:D2}", hourStop, minuteStop);
            button = TEXT_STOP + buttonOffset;
            UI.SetSerialJoin(panel, button, timeStop);
            Debug("Setting " + button + " to " + timeStop);
        }

        void panel_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            // update feedback when panel comes online
            PanelFeedback();
        }

        void panel_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            // check for button press
            if (args.Sig.Type == eSigType.Bool && args.Sig.BoolValue == true)
            {
                uint button = args.Sig.Number - buttonOffset;

                Debug("Button " + button + " pressed.");

                if (button >= BTN_START_MON && button <= BTN_START_SUN)
                {
                    uint index = button - BTN_START_MON;
                    daysStart[index] = !daysStart[index];
                }
                else if (button >= BTN_STOP_MON && button <= BTN_STOP_SUN)
                {
                    uint index = button - BTN_STOP_MON;
                    daysStop[index] = !daysStop[index];
                }
                else switch (button)
                {
                    case BTN_START_HOUR_INC:
                    {
                        hourStart++;
                        if (hourStart > LIMIT_HOUR_MAX)
                            hourStart = 0;
                        break;
                    }
                    case BTN_START_HOUR_DEC:
                    {
                        hourStart--;
                        if (hourStart > LIMIT_HOUR_MAX)
                            hourStart = LIMIT_HOUR_MAX;
                        break;
                    }
                    case BTN_START_MINUTE_INC:
                    {
                        minuteStart++;
                        if (minuteStart > LIMIT_MINUTE_MAX)
                            minuteStart = 0;
                        break;                        
                    }
                    case BTN_START_MINUTE_DEC:
                    {
                        minuteStart--;
                        if (minuteStart > LIMIT_MINUTE_MAX)
                            minuteStart = LIMIT_MINUTE_MAX;
                        break;
                    }
                    case BTN_STOP_HOUR_INC:
                    {
                        hourStop++;
                        if (hourStop > LIMIT_HOUR_MAX)
                            hourStop = 0;
                        break;
                    }
                    case BTN_STOP_HOUR_DEC:
                    {
                        hourStop--;
                        if (hourStop > LIMIT_HOUR_MAX)
                            hourStop = LIMIT_HOUR_MAX;
                        break;
                    }
                    case BTN_STOP_MINUTE_INC:
                    {
                        minuteStop++;
                        if (minuteStop > LIMIT_MINUTE_MAX)
                            minuteStop = 0;
                        break;
                    }
                    case BTN_STOP_MINUTE_DEC:
                    {
                        minuteStop--;
                        if (minuteStop > LIMIT_MINUTE_MAX)
                            minuteStop = LIMIT_MINUTE_MAX;
                        break;
                    }
                }               

                PanelFeedback();                
            }            
        }
    }
}
