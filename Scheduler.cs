using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace FM.Utilities
{
    public class Scheduler 
    {
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
  
    }
}