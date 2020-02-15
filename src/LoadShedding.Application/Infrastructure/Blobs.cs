using System;
using System.Collections.Generic;
using System.Text;

namespace LoadShedding.Application.Infrastructure
{
    public static class Blobs
    {
        public const string CurrentStage = "stage-data/current-stage.txt";
        public const string Schedule = "stage-data/schedule.json";
        public const string StageChangedPeopleToNotify = "notifications/stage-changed-people-to-notify.json";
    }
}
