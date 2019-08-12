using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using Newtonsoft.Json;

using UI = AVPlus.Utils.UI.UserInterfaceHelper;

namespace FM.Utilities
{
    struct SchedulerEvent
    {
        public bool[] days;
        public uint hour, minute;
    }

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
        const uint LIMIT_DAYS = 7;
        const uint LIMIT_HOUR_MAX = 23;
        const uint LIMIT_MINUTE_MAX = 59;
        
        // events
        public const uint EVENT_START = 0;
        public const uint EVENT_STOP = 1;

        // defaults
        const string DEFAULT_FILENAME = "scheduler.json";
        #endregion

        #region class variables

        // debugging
        public bool debug_enable { get; set; }

        BasicTriList panel;
        uint buttonOffset;

        CTimer timer;
        SchedulerEvent[] events = new SchedulerEvent[2];        
        string fileName;

        Action StartMethod, StopMethod;

        #endregion

        #region public methods

        public Scheduler(BasicTriList panel, uint buttonOffset)
        {
            Init(panel, buttonOffset, DEFAULT_FILENAME);
        }

        public Scheduler(BasicTriList panel, uint buttonOffset, string fileName)
        {
            Init(panel, buttonOffset, fileName);            
        }        

        /// <summary>
        /// Starts the internal timer
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if (timer == null)
            {
                timer = new CTimer(TimerCheck, null, 0, 60000);
                if (timer != null)
                    return true;
                else
                    return false;
            }
            else
            {
                timer.Reset();
                return true;
            }
        }

        /// <summary>
        /// Stops the internal timer
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            if (timer != null)
            {
                timer.Stop();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void SetCallbacks (Action StartMethod, Action StopMethod)
        {
            this.StartMethod = StartMethod;
            this.StopMethod = StopMethod;
        }

        /// <summary>
        /// Loads event data from file.
        /// </summary>
        public bool Load()
        {
            try
            {
                string path = String.Format("USER\\{0}", fileName);
                string json = File.ReadToEnd(path, Encoding.Default);

                events = JsonConvert.DeserializeObject<SchedulerEvent[]>(json);

                Debug("Load() loaded data from file.");

                return true;
            }
            catch (Exception e)
            {
                Debug("Load() exception occurred: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Saves current data to file.
        /// </summary>
        public bool Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(events);
                string path = String.Format("USER\\{0}", fileName);

                FileStream stream = File.Create(path);
                stream.Write(json, Encoding.Default);
                stream.Close();

                Debug("Save() saved all data to file.");

                return true;
            }
            catch (Exception e)
            {
                Debug("Save() exception occurred: " + e.Message);
                return false;
            }

        }

        public void RefreshUI()
        {
            PanelFeedback();
        }

        #endregion

        #region private methods
        void Init (BasicTriList panel, uint buttonOffset, string fileName)
        {
            debug_enable = true;

            this.panel = panel;
            this.buttonOffset = buttonOffset;
            this.fileName = fileName;

            this.StartMethod = null;
            this.StopMethod = null;

            events[EVENT_START].days = new bool[LIMIT_DAYS];
            events[EVENT_STOP].days = new bool[LIMIT_DAYS];

            panel.SigChange += new SigEventHandler(panel_SigChange);
            panel.OnlineStatusChange += new OnlineStatusChangeEventHandler(panel_OnlineStatusChange);

            // load data
            Load();

            // start timer
            Start();
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
                UI.SetDigitalJoin(panel, button, events[EVENT_START].days[i]);

                // stop days
                button = (uint)(BTN_STOP_MON + i + buttonOffset);
                UI.SetDigitalJoin(panel, button, events[EVENT_STOP].days[i]);
            }

            // start time text
            string timeStart = String.Format("{0}:{1:D2}", events[EVENT_START].hour, events[EVENT_START].minute);
            button = TEXT_START + buttonOffset;
            UI.SetSerialJoin(panel, button, timeStart);

            // stop time text
            string timeStop = String.Format("{0}:{1:D2}", events[EVENT_STOP].hour, events[EVENT_STOP].minute);
            button = TEXT_STOP + buttonOffset;
            UI.SetSerialJoin(panel, button, timeStop);
        }

        /// <summary>
        /// Checks current system time against stored values and executes callback methods if match found
        /// </summary>
        /// <param name="obj">Not used in this method</param>
        void TimerCheck(object obj)
        {
            int dayCurr = (int)DateTime.Now.DayOfWeek;
            int hourCurr = DateTime.Now.Hour;
            int minuteCurr = DateTime.Now.Minute;
            
            // alter day to make monday first day of week
            if (dayCurr == 0)
                dayCurr = 6;
            else
                dayCurr--;

            Debug("TimerCheck() running. Current day: " + dayCurr + " Current hour: " + hourCurr + ", current minute: " + minuteCurr);

            // check for start
            if (events[EVENT_START].days[dayCurr])
            {
                bool hourMatch = events[EVENT_START].hour == hourCurr;
                bool minuteMatch = events[EVENT_START].minute == minuteCurr;

                if (hourMatch && minuteMatch)
                {
                    Debug("Start triggered");
                    StartMethod();
                }
            }

            // check for stop
            if (events[EVENT_STOP].days[dayCurr])
            {
                bool hourMatch = events[EVENT_STOP].hour == hourCurr;
                bool minuteMatch = events[EVENT_STOP].minute == minuteCurr;

                if (hourMatch && minuteMatch)
                {
                    Debug("Stop triggered");
                    StopMethod();
                }
            }
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

                if (button >= BTN_START_MON && button <= BTN_START_SUN)
                {
                    uint index = button - BTN_START_MON;
                    events[EVENT_START].days[index] = !events[EVENT_START].days[index];
                }
                else if (button >= BTN_STOP_MON && button <= BTN_STOP_SUN)
                {
                    uint index = button - BTN_STOP_MON;
                    events[EVENT_STOP].days[index] = !events[EVENT_STOP].days[index];
                }
                else switch (button)
                {
                    case BTN_START_HOUR_INC:
                    {
                        events[EVENT_START].hour++;
                        if (events[EVENT_START].hour > LIMIT_HOUR_MAX)
                            events[EVENT_START].hour = 0;
                        break;
                    }
                    case BTN_START_HOUR_DEC:
                    {
                        events[EVENT_START].hour--;
                        if (events[EVENT_START].hour > LIMIT_HOUR_MAX)
                            events[EVENT_START].hour = LIMIT_HOUR_MAX;
                        break;
                    }
                    case BTN_START_MINUTE_INC:
                    {
                        events[EVENT_START].minute++;
                        if (events[EVENT_START].minute > LIMIT_MINUTE_MAX)
                            events[EVENT_START].minute = 0;
                        break;
                    }
                    case BTN_START_MINUTE_DEC:
                    {
                        events[EVENT_START].minute--;
                        if (events[EVENT_START].minute > LIMIT_MINUTE_MAX)
                            events[EVENT_START].minute = LIMIT_MINUTE_MAX;
                        break;
                    }
                    case BTN_STOP_HOUR_INC:
                    {
                        events[EVENT_STOP].hour++;
                        if (events[EVENT_STOP].hour > LIMIT_HOUR_MAX)
                            events[EVENT_STOP].hour = 0;
                        break;
                    }
                    case BTN_STOP_HOUR_DEC:
                    {
                        events[EVENT_STOP].hour--;
                        if (events[EVENT_STOP].hour > LIMIT_HOUR_MAX)
                            events[EVENT_STOP].hour = LIMIT_HOUR_MAX;
                        break;
                    }
                    case BTN_STOP_MINUTE_INC:
                    {
                        events[EVENT_STOP].minute++;
                        if (events[EVENT_STOP].minute > LIMIT_MINUTE_MAX)
                            events[EVENT_STOP].minute = 0;
                        break;
                    }
                    case BTN_STOP_MINUTE_DEC:
                    {
                        events[EVENT_STOP].minute--;
                        if (events[EVENT_STOP].minute > LIMIT_MINUTE_MAX)
                            events[EVENT_STOP].minute = LIMIT_MINUTE_MAX;
                        break;
                    }
                }

                PanelFeedback();
            }            
        }

        #endregion
    }
}
