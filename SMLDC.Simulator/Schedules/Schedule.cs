using SMLDC.Simulator.Models.HeartRate;
using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Utilities;
using static SMLDC.Simulator.Utilities.Enums;
using System;
using System.Collections.Generic;

namespace SMLDC.Simulator.Schedules
{
    public class Schedule
    {
        public DateTime? startingDateForReal;

        private AbstractHeartRateGenerator heartRateGenerator;

        public double GetHrBaseEstimate(uint start, uint end)
        {
            return heartRateGenerator.GetHrBaseEstimate(start, end);
        }
        public void SetHeartRateGenerator(AbstractHeartRateGenerator heartRateGenerator)
        {
            this.heartRateGenerator = heartRateGenerator;
        }
        public AbstractHeartRateGenerator GetHeartRateGenerator() { return this.heartRateGenerator; }
        public double GetHeartRate(uint time)
        {
            return this.heartRateGenerator.GetHeartRate(time);
        }



        private List<PatientEvent> MmntsEvents;
        private int currentMmntsIndex = 0;  // de queue wordt nooit ingekort aan de voorkant. De index wordt verhoogd


        private List<PatientEvent> Events;
        private int currentIndex = 0;  // de queue wordt nooit ingekort aan de voorkant. De index wordt verhoogd

        public Schedule() : this(null)
        {
        }
        public Schedule(DateTime? start)
        {
            startingDateForReal = start;
            Events = new List<PatientEvent>();
            MmntsEvents = new List<PatientEvent>();
            isSorted = true;
            currentIndex = 0;
        }




        public void AddAllInsulinEventsFromSchedule(Schedule schedule_to_add) { AddAllXEventsFromSchedule(schedule_to_add, PatientEventType.INSULIN); }
        public void AddAllCarbEventsFromSchedule(Schedule schedule_to_add) { AddAllXEventsFromSchedule(schedule_to_add, PatientEventType.CARBS); }
        private void AddAllXEventsFromSchedule(Schedule schedule_to_add, PatientEventType eventType)
        {
            foreach (PatientEvent pEvt in schedule_to_add.Events)
            {
                if (pEvt.EventType == eventType)
                {
                    bool succes = this.AddUniqueEventWithoutSorting(pEvt);
                    if (!succes)
                    {
                        throw new ArgumentException();
                    }
                }
            }
            this.SortSchedule();
            this.FixScheduleDoubles();
        }




        // lijst gebruiken en die sorteren via functie (e.g. "finalize")
        // GEEN meerdere events op zelfde tijd mogelijk

        //tijdstip zelf telt niet mee. AFTER time...
        public bool TryGetFirstCarbAfterTime(uint time, out int carbTime)
        {
            carbTime = -1;
            PatientEvent evt = GetFirstCarbEventAfterTime(time, out int ndx);
            if (evt != null)
            {
                carbTime = (int) evt.TrueStartTime;
            }
            return (evt != null);
        }

        public PatientEvent GetFirstInsulinEventAfterTime(uint time) { return GetFirstEventAfterTime(time, PatientEventType.INSULIN, out int eventIndex); }
        public PatientEvent GetFirstInsulinEventAfterTime(uint time, out int eventIndex) { return GetFirstEventAfterTime(time, PatientEventType.INSULIN, out eventIndex); }

        public PatientEvent GetFirstInsulinEventAfterTime(int time) { return GetFirstEventAfterTime((uint)time, PatientEventType.INSULIN, out int eventIndex); }
        public PatientEvent GetFirstInsulinEventAfterTime(int time, out int eventIndex) { return GetFirstEventAfterTime((uint)time, PatientEventType.INSULIN, out eventIndex); }

        public PatientEvent GetFirstCarbEventAfterTime(int time) { return GetFirstEventAfterTime((uint)time, PatientEventType.CARBS, out int eventIndex); }
        public PatientEvent GetFirstCarbEventAfterTime(uint time) { return GetFirstEventAfterTime(time, PatientEventType.CARBS, out int eventIndex); }
        public PatientEvent GetFirstCarbEventAfterTime(int time, out int eventIndex) { return GetFirstEventAfterTime((uint)time, PatientEventType.CARBS, out eventIndex); }
        public PatientEvent GetFirstCarbEventAfterTime(uint time, out int eventIndex) { return GetFirstEventAfterTime(time, PatientEventType.CARBS, out eventIndex); }



        public PatientEvent GetFirstEventAfterTime(uint time, PatientEventType ptype, out int eventIndex)
        {
            eventIndex = -1;
            //int carbTime = -1;
            if(Events[Events.Count-1].TrueStartTime < time) { return null; } // te laat.
            try
            {
                int ndx = GetPositionToInsertTimeAt(time);
                if (ndx < 0)
                {
                    ndx = 0; // te vroeg. Zoek vanaf eerste
                }
                for(int i = ndx ; i < Events.Count; i++)
                {
                    if(Events[i].EventType == ptype)
                    {
                        //carbTime = (int) Events[i].StartTime;
                        eventIndex = i;
                        return Events[i];
                    }
                }
                // niet gevonden
                return null;
            }
            catch(Exception)
            {
                return null;
            }
        }



        public PatientEvent GetLastCarbEventBeforeTime(int time) { return GetLastEventBeforeTime(time, PatientEventType.CARBS); }
        public PatientEvent GetLastCarbEventBeforeTime(uint time) { return GetLastEventBeforeTime(time, PatientEventType.CARBS); }
        public PatientEvent GetLastInsulinEventBeforeTime(int time) { return GetLastEventBeforeTime(time, PatientEventType.INSULIN); }
        public PatientEvent GetLastInsulinEventBeforeTime(uint time) { return GetLastEventBeforeTime(time, PatientEventType.INSULIN); }
        private PatientEvent GetLastEventBeforeTime(int time, PatientEventType patientEventType) { 
            if(time < 0)
            {
                throw new ArgumentException("eventtijd kan niet < 0 zijn! time = " + time);
            }
            return GetLastEventBeforeTime((uint)time, patientEventType); 
        }
        private PatientEvent GetLastEventBeforeTime(uint time, PatientEventType patientEventType)
        {
            // terugzoeken vanaf plek waar deze tijd hoort (wbt index in events)
            int ndx = GetPositionToInsertTimeAt(time);
            if (ndx > 0)
            {
                for (int i = ndx - 1; i >= 0; i--)
                {
                    if (this.Events[i].EventType == patientEventType)
                    {
                        return this.Events[i];
                    }
                }
            }
            return null;
        }



        public void RemoveCarbEvents()
        {
            List<PatientEvent> newEvents = new List<PatientEvent>(Events.Count / 2 + 1);
            for (int i = 0; i < Events.Count; i++)
            {
                if (Events[i].EventType != PatientEventType.CARBS)
                {
                    newEvents.Add(Events[i]);
                }
            }
            this.Events = newEvents;
        }



        public void UpdateCarbEventsToValue(double value, uint starttime, uint stoptime)
        { UpdateCarbEventsToValue(value, starttime, (int)stoptime); }
        public void UpdateCarbEventsToValue(double value, uint starttime, int stoptime = -1)
        {
           
            for (int evtNdx = 0; evtNdx < Events.Count; evtNdx++)
            {
                if(Events[evtNdx].TrueStartTime< starttime) { continue; }
                if (stoptime >= 0 && Events[evtNdx].TrueStartTime > stoptime) {
                    return;
                }
                if(Events[evtNdx].EventType == PatientEventType.CARBS)
                {
                    Events[evtNdx].Carb_TrueValue_in_gram = value;
                }
            }
        }
    public void UpdateEventAtTime(uint time, double value)
        {
            int index = GetEventIndexFromTime(time);
            if (index < 0)
            {
                throw new ArgumentException("Schedule::UpdateEventAtTime(" + time + ", ...) --> not present!");
            }
            UpdateEventAtIndex(index, value);
        }

        public void UpdateEventAtIndex(int index, double value)
        {
            this.Events[index].TrueValue = value;
        }



        public PatientEvent MoveEventAtTimeToTime(PatientEvent pEvent, uint newTime)
        {
            int offset = (int)newTime - (int)pEvent.TrueStartTime;
            return MoveEventAtTime(pEvent, offset);
        }
        public PatientEvent MoveEventAtTimeToTime(uint time, uint newTime)
        {
            int offset = (int) newTime - (int) time;
            return MoveEventAtTime(time, offset);
        }

        public PatientEvent MoveEventAtTime(PatientEvent pEvent, int offset)
        {
            uint time = pEvent.TrueStartTime;
            return MoveEventAtTime(time, offset);
        }

        public PatientEvent MoveEventAtTime(uint time, int offset)
        { 
            int index = GetEventIndexFromTime(time);
            if (index < 0) { throw new ArgumentException("Schedule::MoveEventAtTime(" + time + ", ...) --> not present!"); }
            PatientEvent pEvent = Events[index];
            if (offset == 0) { return pEvent; }
            
            int newtime = (int)this.Events[index].TrueStartTime + offset;
            int offsetDir = Math.Sign(offset);
            //check of die tijd vrij is:
            while (this.ContainsTime(newtime))
            {
                // iets minder ver stappen
                newtime -= offsetDir;
            }

            pEvent.TrueStartTime = (uint)Math.Max(0, newtime);
            if(this.Events.Count == 1) { return pEvent; }

            // daadwerkelijk nieuwe tijd - oude tijd is de offset (sign zou omgeklapt kunnen zijn)
            offsetDir = (int)pEvent.TrueStartTime - (int) time;
            
            // sortering fixen!
            int dir = Math.Sign(offsetDir); // 0;

            while ((dir > 0  && index < Events.Count - 1) || (dir < 0 && index > 0))
            {
                // als dir = 1 ga je naar toekomst. Als dan de volgende event - huidige niet 1 is, dan is er iets mis
                // als dir = -1 naar verleden. Als dan vorige - huidig 1 is: fout
                int diff = Math.Sign((int)Events[index + dir].TrueStartTime - (int)Events[index].TrueStartTime);
                if (diff == -dir )
                {
                    //swap
                    PatientEvent tempevt = Events[index];
                    Events[index] = Events[index + dir];
                    Events[index + dir] = tempevt;
                    index += dir;
                   // IntegrityCheck();
                }
                else
                {
                    IntegrityCheck();
                    break;
                }
            }

            // 2 events op zelfde tijdstip voorkomen:
            
            
            IntegrityCheck();

            return pEvent;
           // this.SortSchedule(); //duur!
        }



        public PatientEvent GetEventFromTime(uint time)
        {
            int index = GetEventIndexFromTime(time);
            return this.Events[index];
        }

        public PatientEvent GetMmntEventFromTime(uint time)
        {
            int index = GetMmntEventIndexFromTime(time);
            return (PatientEvent) this.MmntsEvents[index];
        }


        public PatientEvent GetEventFromIndex(int index) { return this.Events[index]; }
        public PatientEvent GetMmntEventFromIndex(int index) { return (PatientEvent) this.MmntsEvents[index]; }


        public int GetEventCount() { return Events.Count; }
        public int GetMmntCount() { return MmntsEvents.Count; }


        public Schedule DeepCopy()
        {
            Schedule newSchedule = new Schedule();
            for (int i = 0; i < this.Events.Count; i++)
            {
                PatientEvent evt_copy = Events[i].CopyEvent();
                newSchedule.Events.Add(evt_copy);
            }
            for (int i = 0; i < this.MmntsEvents.Count; i++)
            {
                PatientEvent evt_copy = MmntsEvents[i].CopyEvent();
                newSchedule.MmntsEvents.Add((PatientEvent) evt_copy);
            }
            newSchedule.isSorted = this.isSorted;
           // newSchedule.stopEvent_time = this.stopEvent_time;
            // hr model ook overnemen:
            newSchedule.heartRateGenerator = this.heartRateGenerator.Copy();
            return newSchedule;
        }



        public void AddStopEvent()
        {
            // zorg ervoor dat de events lijst niet al eerder 'leeg' is, zodat er niet tot de laatste mmnts doorgesim't wordt.
            uint maxtime = MmntsEvents[MmntsEvents.Count - 1].TrueStartTime;
            PatientEvent stopEvent = new PatientEvent(PatientEventType.STOP, maxtime, 0);
            Events.Add(stopEvent);
        }

        private bool isSorted = false;

        public void SortSchedule()
        {
            Events.Sort();
            MmntsEvents.Sort();
            isSorted = true;
        }


        
        public void FixScheduleDoubles()
        {
            return; //
            for (int i = 0; i < Events.Count - 1; i++)
            {
                if (Events[i].TrueStartTime == Events[i + 1].TrueStartTime)
                {
                    // [i] of [i+1] verplaatsen
                    if(i > 0 && Events[i-1].TrueStartTime < Events[i].TrueStartTime-1)
                    {
                        Events[i].TrueStartTime--;
                    }
                    else if(i + 2 < Events.Count && Events[i+2].TrueStartTime > Events[i+1].TrueStartTime + 1)
                    {
                        Events[i + 1].TrueStartTime++;
                    }
                    else
                    {
                        throw new ArgumentException("te krap... geen makkelijke oplossing!");
                    }
                }

            }
            IntegrityCheck();
        }


        public void IntegrityCheck()
        {
            return;
            // sanity check op dubbele events:
            for (int i = 0; i < Events.Count - 1; i++)
            {
                if (Events[i].TrueStartTime >= Events[i + 1].TrueStartTime)
                {
                    throw new ArgumentException("twee events met zelfde time! Foutje gemaakt in AddEventWithoutSorting??");
                }

            }
        }

        public override string ToString()
        {
            return ToString(3, 10);
        }

        public string ToString(int past, int future)
        {
            past = Math.Abs(past); // zodat het met - en + werkt :-)
            future = Math.Abs(future);
            int current_ndx = this.GetEventIndexFromTime(this.GetCurrentTime());
            List<PatientEvent> events2txt = new List<PatientEvent>();
            int teller = 0;

            //toekomst:
            int ndx = current_ndx + 1;
            while (ndx < Events.Count && teller < future)
            {
                PatientEvent evt = this.Events[ndx];
                //if (evt.EventType != PatientEventType.GLUCOSE_MASUREMENT)
                {
                    events2txt.Add(evt);
                    teller++;
                }
                ndx++;
            }

            // verleden:
            teller = 0;
            ndx = current_ndx;
            while (ndx >= 0 && teller < past)
            {
                PatientEvent evt = this.Events[ndx];
               // if (evt.EventType != PatientEventType.GLUCOSE_MASUREMENT)
                {
                    events2txt.Insert(0, evt); //vooraan, want 'past'
                    teller++;
                }
                ndx--;
            }

            string txt = "Schedule{around " + this.GetCurrentTime() + ":\n";
            foreach (PatientEvent evt in events2txt)
            {
                txt += "[" + evt.TrueStartTime + ":" + evt.PatientEventTypeToString() + "]=" + OctaveStuff.MyFormat(evt.TrueValue, 3) /*+ "(" + OctaveStuff.MyFormat(evt.NoisyValue, 3)*/ + ")\n";
            }
            return txt + "}"; 
        }



        public uint GetLastTime()
        {
            return Events[Events.Count - 1].TrueStartTime;
        }

        public uint GetCurrentTime()
        {
            if (currentIndex < 0) { return 0; }
            if (currentIndex < Events.Count)
            {
                return Events[currentIndex].TrueStartTime;
            }
            else return GetLastTime();
        }



        public bool TryGetMmntEvent(uint time, out PatientEvent evt)
        {
            int ndx = GetMmntEventIndexFromTime(time);
            if (ndx < 0)
            {
                evt = null;
                return false;
            }
            else
            {
                evt = (PatientEvent) MmntsEvents[ndx];
                return true;
            }
        }

        public bool TryGetEvent(uint time, out PatientEvent evt)
        {
            int ndx = GetEventIndexFromTime(time);
            if (ndx < 0)
            {
                evt = null;
                return false;
            }
            else
            {
                evt = Events[ndx];
                return true;
            }
        }

        public bool ContainsTime(PatientEvent evt) { return ContainsTime(evt.TrueStartTime); }
        public bool ContainsTime(int time) { return ContainsTime((uint)time); }
        public bool ContainsTime(uint time)
        {
            int ndx = GetEventIndexFromTime(time);
            return (ndx >= 0);
        }


        //zoek plek. Aanname is dat alles sorted is.
        public int GetEventIndexFromTime(uint time)
        {
            return GetEventIndexFromTime(time, false);
        }
        public int GetMmntEventIndexFromTime(uint time)
        {
            return GetEventIndexFromTime(time, true);
        }

        private int GetEventIndexFromTime(uint time, bool isMmnt)
        {
            List<PatientEvent> eventsList = null;
            if(isMmnt)
            {
                eventsList = MmntsEvents;
            }
            else
            {
                eventsList = Events;
            }

            if(eventsList.Count == 0) {
                return -1;
            }
            //zoek ... binair
            int ndx_laag = 0;
            int ndx_hoog = eventsList.Count - 1;
            while (true)
            {
                int ndx_mid = (ndx_hoog + ndx_laag) / 2; //halvewege
                if (time == eventsList[ndx_mid].TrueStartTime)
                {
                    return ndx_mid;
                }
                else if (time < eventsList[ndx_mid].TrueStartTime)
                {
                    ndx_hoog = ndx_mid;
                }
                else
                {
                    ndx_laag = ndx_mid;
                }

                if (ndx_laag == ndx_hoog)
                {
                    if (time == eventsList[ndx_laag].TrueStartTime)
                    {
                        return ndx_laag;
                    }
                    else
                    {
                        return -1;
                    }
                }
                if (ndx_hoog - ndx_laag == 1)
                {
                    if (time == eventsList[ndx_laag].TrueStartTime)
                    {
                        return ndx_laag;
                    }
                    else if (time == eventsList[ndx_hoog].TrueStartTime)
                    {
                        return ndx_hoog;
                    }
                    return -1;
                }
            }
            //return -1;
        }


        //zoek plek. Aanname is dat alles sorted is.
        // vindt de juiste plek (wbt sortering) voor inserten van nieuw event
        //public int GetPositionToInsertAt(PatientEvent evt) { return GetPositionToInsertTimeAt(evt.TrueStartTime); }

        private int GetPositionToInsertTimeAt_Mmnt(uint time)
        {
            return GetPositionToInsertTimeAt(time, true);
        }
        private int GetPositionToInsertTimeAt(uint time)
        {
            return GetPositionToInsertTimeAt(time, false);
        }
        
        private int GetPositionToInsertTimeAt(uint time, bool isMmnt)
        {
            List<PatientEvent> eventLijst = null;
            if (isMmnt)
            {
                eventLijst = MmntsEvents;
            }
            else
            {
                eventLijst = Events;
            }

            if (eventLijst.Count == 0)
            {
                return 0; //leeg, dus plek 0 is een vrij logiche plek om de nieuwe te plaaten
            }
            if (eventLijst.Count == 1)
            {
                // check of voor of na (of op) deze tijd.
                if (time <= eventLijst[0].TrueStartTime)
                {
                    return 0; //ervoor, of vervangen
                }
                else
                {
                    return 1; //erna
                }
            }


            //zoek ... binair
            int ndx_laag = 0;
            int ndx_hoog = eventLijst.Count - 1;

            //randen:
            if (time > eventLijst[ndx_hoog].TrueStartTime)
            {
                return eventLijst.Count; //achteraan
            }
            else if (time < eventLijst[0].TrueStartTime)
            {
                return 0; //voor begin
            }


            while (true)
            {
                int ndx_mid = (ndx_hoog + ndx_laag) / 2; //halvewege
                if (time == eventLijst[ndx_mid].TrueStartTime)
                {
                    return ndx_mid; // op dit punt inserten (zodat dit item opschuift)
                }
                else if (time < eventLijst[ndx_mid].TrueStartTime)
                {
                    ndx_hoog = ndx_mid;
                }
                else
                {
                    ndx_laag = ndx_mid;
                }

                if (ndx_laag == ndx_hoog)
                {
                    return ndx_laag; // niet gevonden, of dit is de plek. Hoe dan ook, hier inserten
                }
                if (ndx_hoog - ndx_laag == 1) // eindpunt. Check welke van de twee (of geen) er een match is
                {
                    if (time <= eventLijst[ndx_laag].TrueStartTime)
                    {
                        return ndx_laag; //hier komt ie
                    }
                    else if (time <= eventLijst[ndx_hoog].TrueStartTime)
                    {
                        return ndx_hoog; //hier komt ie
                    }
                    // als we hier zijn, is de time niet gevonden. Er boven
                    return ndx_hoog + 1;
                }
            }
        }


        

        // false als plaatje al bezet is (en dus wordt ie dan niet toegevoegd)

        // AANNAME: de events zijn nooit dubbelen van elkaar!
        public bool AddUniqueEventWithoutSorting(PatientEvent patientEvent)
        {
            return AddEvent(new PatientEvent(patientEvent), false);
        }
        public bool AddUniqueEventWithoutSorting(PatientEventType eventType, uint startTime, double trueValue)
        {
            return AddEvent(new PatientEvent(eventType, startTime, trueValue), false);
        }
        public bool AddEvent(PatientEventType eventType, uint startTime, double trueValue)
        {
            return AddEvent(new PatientEvent(eventType, startTime, trueValue), true);
        }
        public bool AddEvent(PatientEvent patientEvent)
        {
            return AddEvent(new PatientEvent(patientEvent), true);
        }

        // wiggle: toevoegen, maar als die plek al bezet is, verplaatsen
        public void AddEventWiggle(PatientEventType eventType, uint startTime, double trueValue)
        {
            AddEventWiggle(new PatientEvent(eventType, startTime, trueValue));
        }
        public void AddEventWiggle(PatientEvent patientEvent)
        {
            if(!isSorted) { this.SortSchedule(); }
            bool succes = AddEvent(patientEvent, true);
            while(!succes)
            {
                //iets verplaatsen totdat het past
                int teller = 1;
                int insertTime = (int)patientEvent.TrueStartTime;
                while (!succes)
                {
                    // op deze manier zigzaggend om gewenste tijd heen proberen toe te voegen: offset 1, -2, 3, -4, etc..
                    insertTime += (teller * (teller % 2 == 0 ? -1 : 1));
                    teller++;
                    if (insertTime >= 0)
                    {
                        patientEvent.TrueStartTime = (uint)insertTime;
                        succes = AddEvent(patientEvent, true);
                        this.IntegrityCheck();
                    }
                    //if(!succes)
                    //{

                    //}
                }
            }
            IntegrityCheck();
        }


        private bool AddEvent(PatientEvent patientEvent, bool doSort)
        {
            List<PatientEvent> eventLijst = null;
            bool isMmnt = (patientEvent.EventType == PatientEventType.GLUCOSE_MASUREMENT);
            if (isMmnt)
            {
                eventLijst = MmntsEvents;
            }
            else
            {
                eventLijst = Events;
            }


            if (doSort)
            {
                if (eventLijst.Count == 0)
                {
                    eventLijst.Add(patientEvent);
                    return true;
                }
                //zoek plek. Aanname is dat alles sorted is.
                int ndx_to_insert = GetPositionToInsertTimeAt(patientEvent.TrueStartTime, isMmnt);
                if (ndx_to_insert < eventLijst.Count && eventLijst[ndx_to_insert].TrueStartTime == patientEvent.TrueStartTime)
                {
                    //bezet 
                    return false;
                }
                eventLijst.Insert(ndx_to_insert, patientEvent); //rest wordt opgeschoven. Weer gesorteerd
                return true;
            }
            else
            {
                isSorted = false;
                // voeg op goedkoopste plek toe. Moet later toch nog gesorteerd worden.
                eventLijst.Add(patientEvent);
                // hoe voorkomen we dubbele tijden?? Tijden in een hashset verzamelen?
                return true;
            }
        }
        




        public bool IsEmpty(bool negeerMmnts=true)
        {
            if (negeerMmnts)
            {
                return (Events.Count == 0 || currentIndex >= Events.Count);
            }
            else
            {
                if (Events.Count > 0 && currentIndex < Events.Count) { return false; }
                if (MmntsEvents.Count > 0 && currentMmntsIndex < MmntsEvents.Count) { return false; }
                return true;
            }
        }

        public PatientEvent PopEvent(bool negeerMmnts = true)
        {
            PatientEvent evt = PeekEvent();
            if(negeerMmnts)
            {
                currentIndex++;
                return evt;
            }
            PatientEvent mmntEvt = PeekMmntsEvent();
            if (evt == null) 
            {
                currentMmntsIndex++;
                return mmntEvt; 
            }
            else if (mmntEvt == null) 
            {
                currentIndex++;
                return evt;
            }
            if (evt.TrueStartTime <= mmntEvt.TrueStartTime)
            {
                currentIndex++;
                return evt;
            }
            else
            {
                currentMmntsIndex++;
                return mmntEvt;
            }
        }



        private PatientEvent PeekEvent()
        {
            if (currentIndex >= Events.Count)
            {
                return null;
            }
            PatientEvent evt = Events[currentIndex];
            return evt;
        }
        private PatientEvent PeekMmntsEvent()
        {
            if (currentMmntsIndex >= MmntsEvents.Count)
            {
                return null;
            }
            PatientEvent evt = MmntsEvents[currentMmntsIndex];
            return evt;
        }




        public void CropScheduleToDateTime(DateTime start, DateTime end)
        {
            // kan alleen als er een startdate is!
            if(this.startingDateForReal == null)
            {
                throw new ArgumentException("er is geen startingDate... dit is geen schema gebaseerd op echte data!");
            }
            if(start < this.startingDateForReal.Value)
            {
                throw new ArgumentException("crop start <startingDateForReal ");
            }
            int offset = (int)Math.Round((start - startingDateForReal.Value).TotalMinutes);
            if(offset < 0)
            {
                throw new ArgumentException("dit zou niet moeten mogen!");
            }
            int endTijd = (int)Math.Round((end - startingDateForReal.Value).TotalMinutes) - offset;
            List<PatientEvent> eventLijst = null;
            for (int j = 0; j < 2; j++)
            {
                if (j == 0)
                {
                    eventLijst = MmntsEvents;
                }
                else
                {
                    eventLijst = Events;
                }
                List<int> ndxToRemove = new List<int>();
                for (int i = 0; i < eventLijst.Count; i++)
                {
                    int newStartTime = (int)eventLijst[i].TrueStartTime - offset;
                    if (newStartTime >= 0 && newStartTime <= endTijd)
                    {
                        eventLijst[i].TrueStartTime = (uint)newStartTime;
                    }
                    else
                    {
                        ndxToRemove.Add(i);
                    }
                }
                for (int i = ndxToRemove.Count - 1; i >= 0; i--)
                {
                    eventLijst.RemoveAt(ndxToRemove[i]);
                }
            }
            startingDateForReal = start;
        }


        //returns cropped copy of this schedule.
        // aanname: gesorteerde schedule.
        public Schedule CropSchedule2Copy(uint startTime, uint stopTime, bool gebruikGlucMmnts)
        {
            Schedule newSchedule = new Schedule();
            for (int i = 0; i < Events.Count; i++)
            {
                PatientEvent evt = Events[i];
                if (evt.TrueStartTime >= startTime && evt.TrueStartTime <= stopTime)
                {
                    PatientEvent evt_copy = (PatientEvent)evt.CopyEvent();
                    evt_copy.TrueStartTime = evt_copy.TrueStartTime;
                    newSchedule.Events.Add(evt_copy);
                }
            }

            if(gebruikGlucMmnts)
            {
                for (int i = 0; i < MmntsEvents.Count; i++)
                {
                    PatientEvent evt = MmntsEvents[i];
                    if (evt.TrueStartTime >= startTime && evt.TrueStartTime <= stopTime)
                    {
                        PatientEvent evt_copy = (PatientEvent)evt.CopyEvent();
                        evt_copy.TrueStartTime = evt_copy.TrueStartTime;
                        newSchedule.MmntsEvents.Add(evt_copy);
                    }
                }
            }

            newSchedule.heartRateGenerator = this.heartRateGenerator.Copy();
            return newSchedule;
        }





        public List<uint> GetCarbTimes(uint starttime, uint stopTime)
        {
            if(!isSorted) { this.SortSchedule(); }

            List<uint> times = new List<uint>();
            PatientEvent evt = this.GetFirstCarbEventAfterTime(starttime, out int eventIndex);
            if (evt == null)
            {
                return times;
            }
            for (int i = eventIndex; i < this.Events.Count; i++)
            {
                evt = this.Events[i];
                times.Add(evt.TrueStartTime);
            }
            return times;
        }
        // geeft de lijst van carb events in de range [starttime, stoptime>
        // met in elk tuple ook het 'bereik' van die carb input (by default tot de volgende carb, tenzij minimaleTijd > 0, want
        // in dat geval is de tijdsduur na de carbinput minimaal minimaleTijd.)


        public List<Tuple<PatientEvent, uint>> GetCarbPieken(uint starttime, uint stopTime, uint minimaleTijd = 0)
        {
            List<Tuple<PatientEvent, uint>> pieken = new List<Tuple<PatientEvent, uint>>();
            PatientEvent evt = this.GetFirstCarbEventAfterTime(starttime, out int eventIndex);
            if (evt == null)
            {
                return pieken;
            }
            for(int i = eventIndex + 1; i < this.Events.Count; i++)
            {
                PatientEvent newEvent = Events[i];
                if (newEvent.TrueStartTime > stopTime)
                {
                    break;
                }
                if(newEvent.EventType != PatientEventType.CARBS)
                {
                    continue;
                }
                uint actualStopTime = newEvent.TrueStartTime - 1;
                //uint minimaleTijd = (uint)(60 * 3.5);
                if (Math.Abs(actualStopTime - evt.TrueStartTime) < minimaleTijd)
                {
                    // zodat we minimaal iets aan effect hebben (ook als 2 carb events vlak op elkaar zitten)
                    actualStopTime = Math.Min(stopTime, (evt.TrueStartTime + minimaleTijd)); 
                }
                pieken.Add(new Tuple<PatientEvent, uint>(evt, actualStopTime));
                evt = newEvent;
            }
            pieken.Add(new Tuple<PatientEvent, uint>(evt, stopTime));
            return pieken;
        }




        public List<Tuple<PatientEvent, uint>> GetInsulineEvents()
        {
            return GetInsulineEvents(0, GetLastTime(), 1);
        }

        public List<Tuple<PatientEvent, uint>> GetInsulineEvents(uint starttime, uint stopTime, uint minimaleTijd = 0)
        {
            List<Tuple<PatientEvent, uint>> insEvents = new List<Tuple<PatientEvent, uint>>();
            PatientEvent evt = this.GetFirstInsulinEventAfterTime(starttime, out int eventIndex);
            if (evt == null)
            {
                return insEvents;
            }
            for (int i = eventIndex + 1; i < this.Events.Count; i++)
            {
                PatientEvent newEvent = Events[i];
                if (newEvent.TrueStartTime > stopTime)
                {
                    break;
                }
                if (newEvent.EventType != PatientEventType.INSULIN)
                {
                    continue;
                }
                uint actualStopTime = newEvent.TrueStartTime - 1;
                //uint minimaleTijd = (uint)(60 * 3.5);
                if (Math.Abs(actualStopTime - evt.TrueStartTime) < minimaleTijd)
                {
                    // zodat we minimaal iets aan effect hebben (ook als 2 carb events vlak op elkaar zitten)
                    actualStopTime = Math.Min(stopTime, (evt.TrueStartTime + minimaleTijd));
                }
                insEvents.Add(new Tuple<PatientEvent, uint>(evt, actualStopTime));
                evt = newEvent;
            }
            insEvents.Add(new Tuple<PatientEvent, uint>(evt, stopTime));
            return insEvents;
        }



    }
}
