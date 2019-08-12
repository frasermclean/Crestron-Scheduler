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

namespace FM.Scheduler
{
    struct SchedulerEvent
    {
        public bool[] days;
        public uint hour, minute;
    }

    static class DigitalJoins
    {
        // auto start buttons
        public const uint StartDayMonday = 1;
        public const uint StartDayTuesday = 2;
        public const uint StartDayWednesday = 3;
        public const uint StartDayThursday = 4;
        public const uint StartDayFriday = 5;
        public const uint StartDaySaturday = 6;
        public const uint StartDaySunday = 7;
        public const uint StartHourIncrease = 8;
        public const uint StartHourDecrease = 9;
        public const uint StartMinuteIncrease = 10;
        public const uint StartMinuteDecrease = 11;

        // auto stop buttons
        public const uint StopDayMonday = 12;
        public const uint StopDayTuesday = 13;
        public const uint StopDayWednesday = 14;
        public const uint StopDayThursday = 15;
        public const uint StopDayFriday = 16;
        public const uint StopDaySaturday = 17;
        public const uint StopDaySunday = 18;
        public const uint StopHourIncrease = 19;
        public const uint StopHourDecrease = 20;
        public const uint StopMinuteIncrease = 21;
        public const uint StopMinuteDecrease = 22;
    }

    static class SerialJoins
    {
        public const uint StartText = 1;
        public const uint StopText = 1;
    }

    public class Scheduler
    {
        #region constants
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

        #region Class variables
        // debugging
        public bool debug_enable { get; set; }

        BasicTriList panel;
        uint buttonOffset;

        CTimer timer;
        SchedulerEvent[] events = new SchedulerEvent[2];        
        string fileName;

        Action StartMethod, StopMethod;
        #endregion

        #region Properties
        public bool TraceEnabled { get; set; }
        public string TraceName { get; set; }
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

                Trace("Load() loaded data from file.");

                return true;
            }
            catch (Exception e)
            {
                Trace("Load() exception occurred: " + e.Message);
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

                Trace("Save() saved all data to file.");

                return true;
            }
            catch (Exception e)
            {
                Trace("Save() exception occurred: " + e.Message);
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
        void Trace(string message)
        {
            if (TraceEnabled)
                CrestronConsole.PrintLine(String.Format("[{0}] {1}", TraceName, message.Trim()));
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
                button = (uint)(DigitalJoins.StartDayMonday + i + buttonOffset);
                UI.SetDigitalJoin(panel, button, events[EVENT_START].days[i]);

                // stop days
                button = (uint)(DigitalJoins.StopDayMonday + i + buttonOffset);
                UI.SetDigitalJoin(panel, button, events[EVENT_STOP].days[i]);
            }

            // start time text
            string timeStart = String.Format("{0}:{1:D2}", events[EVENT_START].hour, events[EVENT_START].minute);
            button = SerialJoins.StartText + buttonOffset;
            UI.SetSerialJoin(panel, button, timeStart);

            // stop time text
            string timeStop = String.Format("{0}:{1:D2}", events[EVENT_STOP].hour, events[EVENT_STOP].minute);
            button = SerialJoins.StopText + buttonOffset;
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

            Trace("TimerCheck() running. Current day: " + dayCurr + " Current hour: " + hourCurr + ", current minute: " + minuteCurr);

            // check for start
            if (events[EVENT_START].days[dayCurr])
            {
                bool hourMatch = events[EVENT_START].hour == hourCurr;
                bool minuteMatch = events[EVENT_START].minute == minuteCurr;

                if (hourMatch && minuteMatch)
                {
                    Trace("Start triggered");
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
                    Trace("Stop triggered");
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

                if (button >= DigitalJoins.StartDayMonday && button <= DigitalJoins.StartDaySunday)
                {
                    uint index = button - DigitalJoins.StartDayMonday;
                    events[EVENT_START].days[index] = !events[EVENT_START].days[index];
                }
                else if (button >= DigitalJoins.StopDayMonday && button <= DigitalJoins.StopDaySunday)
                {
                    uint index = button - DigitalJoins.StopDayMonday;
                    events[EVENT_STOP].days[index] = !events[EVENT_STOP].days[index];
                }
                else switch (button)
                {
                    case DigitalJoins.StartHourIncrease:
                    {
                        events[EVENT_START].hour++;
                        if (events[EVENT_START].hour > LIMIT_HOUR_MAX)
                            events[EVENT_START].hour = 0;
                        break;
                    }
                    case DigitalJoins.StartHourDecrease:
                    {
                        events[EVENT_START].hour--;
                        if (events[EVENT_START].hour > LIMIT_HOUR_MAX)
                            events[EVENT_START].hour = LIMIT_HOUR_MAX;
                        break;
                    }
                    case DigitalJoins.StartMinuteIncrease:
                    {
                        events[EVENT_START].minute++;
                        if (events[EVENT_START].minute > LIMIT_MINUTE_MAX)
                            events[EVENT_START].minute = 0;
                        break;
                    }
                    case DigitalJoins.StartMinuteDecrease:
                    {
                        events[EVENT_START].minute--;
                        if (events[EVENT_START].minute > LIMIT_MINUTE_MAX)
                            events[EVENT_START].minute = LIMIT_MINUTE_MAX;
                        break;
                    }
                    case DigitalJoins.StopHourIncrease:
                    {
                        events[EVENT_STOP].hour++;
                        if (events[EVENT_STOP].hour > LIMIT_HOUR_MAX)
                            events[EVENT_STOP].hour = 0;
                        break;
                    }
                    case DigitalJoins.StopHourDecrease:
                    {
                        events[EVENT_STOP].hour--;
                        if (events[EVENT_STOP].hour > LIMIT_HOUR_MAX)
                            events[EVENT_STOP].hour = LIMIT_HOUR_MAX;
                        break;
                    }
                    case DigitalJoins.StopMinuteIncrease:
                    {
                        events[EVENT_STOP].minute++;
                        if (events[EVENT_STOP].minute > LIMIT_MINUTE_MAX)
                            events[EVENT_STOP].minute = 0;
                        break;
                    }
                    case DigitalJoins.StopMinuteDecrease:
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
