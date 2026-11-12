using System.Collections.Generic;
using UnityEngine;

namespace Paridhwani
{
    // Runtime binding supports the existing benchmark and additive scenes without
    // regenerating or replacing the user's current world.
    public sealed class VillageEnvironmentalAudio : MonoBehaviour
    {
        sealed class Emitter
        {
            public Transform anchor;
            public AudioSource source;
            public AudioClip clip;
            public float next, min, max, gain;
        }
        readonly List<Emitter> emitters = new List<Emitter>();
        readonly List<VillageRecordedLoop> vehicles = new List<VillageRecordedLoop>();
        readonly List<VillageRecordedLoop> autos = new List<VillageRecordedLoop>();
        readonly List<VillageRecordedLoop> boilers = new List<VillageRecordedLoop>();
        TeaAmbience preferences;
        TeaStallSession session;
        AudioSource foley;
        TeaProgress previous;
        string mode;
        bool drink, meal, trunk, library, deed;
        bool bound;

        public void Bind(TeaAmbience ambience, TeaStallSession owner)
        { preferences = ambience; session = owner; foley = VillageRecordedAudio.Source(gameObject); }

        void Add(Transform anchor, string sound, float gain, float min, float max, float radius)
        {
            if (!anchor) return;
            var go = new GameObject("Recorded " + sound); go.transform.SetParent(transform, false);go.transform.position=anchor.position;
            emitters.Add(new Emitter { anchor=anchor, source=VillageRecordedAudio.Source(go,true,radius),
                clip=VillageRecordedAudio.Clip(sound), gain=gain, min=min, max=max, next=Time.unscaledTime+Random.Range(min,max) });
        }

        void BindWorld()
        {
            if (!session || session.Progress==null || session.villagers==null) return;
            bound=true;
            foreach(var npc in session.villagers)
            {
                if(!npc)continue;
                if(npc.role==VillageNpcRole.TeaVendor || npc.role==VillageNpcRole.MessOwner)
                {
                    Add(npc.transform,"Utensil",.16f,12,26,9);
                    boilers.Add(LoopAt(npc.transform,"Boiler",7));
                }
                if(npc.role==VillageNpcRole.AutoDriver || npc.role==VillageNpcRole.CheckpostDriver)
                    autos.Add(LoopAt(npc.transform,"Auto",13));
                if(npc.role==VillageNpcRole.TempleKeeper) Add(npc.transform,"TempleBell",.2f,48,90,25);
                if(npc.role==VillageNpcRole.RiverKeeper) Add(npc.transform,"Frog",.07f,15,32,13);
                if(npc.role==VillageNpcRole.ForestGuard || npc.role==VillageNpcRole.Kamalamma)
                { Add(npc.transform,"Bird",.09f,18,35,18); Add(npc.transform,"Cricket",.055f,4,9,14); }
            }
            // The parked bus and auto do not idle forever. Bus audio is limited
            // to arrival; the auto only runs while a travel activity is shown.
            foreach(var t in Object.FindObjectsByType<Transform>())
            {
                if(t.name!="Parked rural bus")continue;
                var go=new GameObject("Recorded bus idle");go.transform.SetParent(t,false);
                var loop=go.AddComponent<VillageRecordedLoop>();loop.Configure("Bus",true,20);vehicles.Add(loop);
            }
        }
        VillageRecordedLoop LoopAt(Transform anchor,string asset,float radius)
        {
            var go=new GameObject("Recorded "+asset);go.transform.SetParent(transform,false);go.transform.position=anchor.position;
            var loop=go.AddComponent<VillageRecordedLoop>();loop.Configure(asset,true,radius);return loop;
        }
        float EnvironmentGain => preferences && preferences.isActiveAndEnabled && !preferences.muted && Application.isFocused ? PresentationPolicy.Volume(preferences.volume):0;
        void Play(string name,float gain=.4f)
        {
            if(!foley || !Application.isFocused || EnvironmentGain<=0)return;
            var clip=VillageRecordedAudio.Clip(name);if(clip)foley.PlayOneShot(clip,gain);
        }

        void Update()
        {
            if(!bound)BindWorld();
            if(!session || session.Progress==null || !preferences)return;
            var p=session.Progress;
            string currentMode=session.ui?session.ui.Mode:"";
            float duck=currentMode=="conversation" || currentMode=="arrival-intro"?.45f:1;
            foreach(var e in emitters)
            {
                bool audible=EnvironmentGain>0 && e.anchor && e.anchor.gameObject.activeInHierarchy;
                e.source.volume=audible?e.gain*EnvironmentGain*duck:0;
                if(!audible){e.source.Stop();continue;}
                e.source.transform.position=e.anchor.position;
                if(Time.unscaledTime>=e.next)
                {
                    e.next=Time.unscaledTime+Random.Range(e.min,e.max);
                    // No distant one-shots, overlapping samples, or random bells during dialogue.
                    if(currentMode!="conversation" && currentMode!="arrival-intro" && preferences.listener &&
                        Vector3.Distance(preferences.listener.position,e.anchor.position)<e.source.maxDistance && e.clip && !e.source.isPlaying)
                        e.source.PlayOneShot(e.clip);
                }
            }
            foreach(var vehicle in vehicles)if(vehicle)vehicle.Gain=!p.arrivalAcknowledged && currentMode!="main-menu"?EnvironmentGain*.14f:0;
            bool travelling=currentMode=="evidence" && (session.ActiveActivity==VillageActivity.CheckpostTravel || session.ActiveActivity==VillageActivity.VillageReturn);
            foreach(var auto in autos)if(auto)auto.Gain=travelling?EnvironmentGain*.15f:0;
            foreach(var boiler in boilers)if(boiler)boiler.Gain=EnvironmentGain*duck*.13f;
            foley.volume=EnvironmentGain;
            if(EnvironmentGain<=0)foley.Stop();
            // Seed on restore/practice/new visit; never replay old rewards on load.
            if(!ReferenceEquals(previous,p)) {previous=p;Remember(p,currentMode);return;}
            if(p.drinkOrdered && !drink){Play("Pour");Play("Cup",.25f);}
            if(p.mealOrdered && !meal)Play("Utensil",.3f);
            if(p.trunkOpened && !trunk){Play("Latch");Play("Door",.25f);}
            if(p.libraryAccessGranted && !library)Play("Door",.3f);
            if(p.deedRetrieved && !deed)Play("Paper",.3f);
            if(currentMode!=mode)
            {
                if(currentMode=="notebook")Play("NotebookOpen",.25f);
                else if(mode=="notebook")Play("NotebookClose",.2f);
                else if(currentMode=="evidence")Play("Paper",.18f);
            }
            Remember(p,currentMode);
        }
        void Remember(TeaProgress p,string currentMode)
        {drink=p.drinkOrdered;meal=p.mealOrdered;trunk=p.trunkOpened;library=p.libraryAccessGranted;deed=p.deedRetrieved;mode=currentMode;}
        void OnDisable()
        {
            if(foley)foley.Stop();foreach(var e in emitters)if(e.source)e.source.Stop();
            foreach(var v in vehicles)if(v)v.Gain=0;
            foreach(var v in autos)if(v)v.Gain=0;
            foreach(var v in boilers)if(v)v.Gain=0;
        }
    }
}
