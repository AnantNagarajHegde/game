using System;
using System.Collections.Generic;
using Paridhwani;

public static class VendorWorkChecks
{
    static int checks;
    static void Check(bool value,string why) {if(!value)throw new Exception(why);checks++;}
    static bool Clear(float a,float b,float c,float d) => true;
    static bool Near(float a,float b) => Math.Abs(a-b)<.0001f;

    static void Interruption(VendorWorkRole role)
    {
        var a=new VendorWorkRoutine(role,317);var b=new VendorWorkRoutine(role,317);
        for(int i=0;i<850;i++)
        {
            a.Tick(.05f,false,Clear);b.Tick(.05f,false,Clear);
            if(i%31!=0)continue;
            float x=a.X,z=a.Z,time=a.ActionTime,face=a.Facing;var task=a.Task;
            int calls=0;
            // Modal/interaction suspension must retain the interrupted step, including travel.
            for(int j=0;j<100;j++)a.Tick(.1f,true,(ox,oz,nx,nz)=>{calls++;return true;});
            Check(Near(a.X,x)&&Near(a.Z,z)&&Near(a.ActionTime,time)&&Near(a.Facing,face)&&a.Task==task&&calls==0,"paused work is entirely inert");
            Check(Near(a.X,b.X)&&Near(a.Z,b.Z)&&a.Task==b.Task&&Near(a.ActionTime,b.ActionTime),"resume preserves the same timeline as uninterrupted work");
        }
        a.Reset();var fresh=new VendorWorkRoutine(role,317);
        Check(a.X==0&&a.Z==0&&a.Task==VendorWorkTask.Rest&&a.ActionTime==0,"restoration resets transient presentation without a save field");
        a.Tick(.05f,false,Clear);fresh.Tick(.05f,false,Clear);
        Check(Near(a.X,fresh.X)&&Near(a.Z,fresh.Z),"reset does not restore an old offset over a restored actor");
    }

    static void Obstruction(VendorWorkRole role)
    {
        var a=new VendorWorkRoutine(role,173);
        for(int i=0;i<400;i++)a.Tick(.1f,false,(ox,oz,nx,nz)=>false);
        Check(a.X==0&&a.Z==0&&a.Task==VendorWorkTask.Rest&&a.ActionTime==0,"blocked route cannot walk or perform work remotely");
        a.Tick(.1f,false,null);Check(a.X==0&&a.Z==0,"missing occupancy policy fails closed");
        a.Tick(.1f,false,Clear);Check(a.X!=0||a.Z!=0,"routine resumes after obstruction clears");
        float x=a.X,z=a.Z,t=a.ActionTime;
        foreach(float dt in new[]{0f,-1f,float.NaN,float.PositiveInfinity})a.Tick(dt,false,Clear);
        Check(a.X==x&&a.Z==z&&a.ActionTime==t,"invalid elapsed time cannot move or advance work");
        a.Tick(20,false,Clear);
        Check(Math.Sqrt((a.X-x)*(a.X-x)+(a.Z-z)*(a.Z-z))<=VendorWorkRoutine.Speed*VendorWorkRoutine.MaximumStepSeconds+.00001,"long frame is bounded rather than teleporting");
    }

    static bool Intersects(float x,float z,float minX,float maxX,float minZ,float maxZ)
    {
        float dx=Math.Max(minX-x,Math.Max(0,x-maxX)),dz=Math.Max(minZ-z,Math.Max(0,z-maxZ));
        return dx*dx+dz*dz<.28f*.28f;
    }

    static void LongRun(VendorWorkRole role,int seed)
    {
        var a=new VendorWorkRoutine(role,seed);var tasks=new HashSet<VendorWorkTask>();
        int boundFailures=0,speedFailures=0,footprintFailures=0;
        bool clearedTeaCounter=false;
        for(int i=0;i<12000;i++)
        {
            float oldX=a.X,oldZ=a.Z,dt=i%3==0?.033f:i%3==1?.075f:.02f;
            a.Tick(dt,false,Clear);tasks.Add(a.Task);
            if(a.X*a.X+a.Z*a.Z>VendorWorkRoutine.MaximumRadius*VendorWorkRoutine.MaximumRadius || !float.IsFinite(a.X)||!float.IsFinite(a.Z))boundFailures++;
            if(Math.Sqrt((a.X-oldX)*(a.X-oldX)+(a.Z-oldZ)*(a.Z-oldZ))>VendorWorkRoutine.Speed*dt+.00001)speedFailures++;
            if(role==VendorWorkRole.Tea)
            {
                float x=-4.3f+a.X,z=1.3f+a.Z;
                // Authored starting anchor already clips the counter. Once escaped,
                // every later anchor/segment must clear counter, boiler and bench.
                bool counter=Intersects(x,z,-6.2f,-2.2f,.51f,1.37f);
                if(!counter)clearedTeaCounter=true;
                if((clearedTeaCounter&&counter)||Intersects(x,z,-7.525f,-5.875f,2.36f,2.84f)||Intersects(x,z,-5.79f,-5.21f,.65f,1.23f))footprintFailures++;
            }
            else
            {
                float x=8.15f+a.X,z=11.1f+a.Z;
                if(Intersects(x,z,6.175f,10.025f,9.69f,10.51f)||Intersects(x,z,9.7f,10.5f,10.2f,11f))footprintFailures++;
            }
        }
        Check(boundFailures==0,"long run stays inside reachable work patch");
        Check(speedFailures==0,"all travel samples respect walking speed");
        Check(footprintFailures==0,"authored horizontal work paths clear counter/bench/boiler/crate footprints after initial escape");
        if(role==VendorWorkRole.Tea)Check(clearedTeaCounter,"tea worker leaves pre-existing counter overlap");
        Check(tasks.Contains(VendorWorkTask.Walk)&&tasks.Contains(VendorWorkTask.Rest),"travel alternates with rest");
        Check(tasks.Contains(role==VendorWorkRole.Tea?VendorWorkTask.CheckBoiler:VendorWorkTask.CheckLedger),"first occupational action reached repeatedly");
        Check(tasks.Contains(role==VendorWorkRole.Tea?VendorWorkTask.ArrangeCups:VendorWorkTask.AdjustScales),"second occupational action reachable");
    }

    public static int Main()
    {
        try
        {
            foreach(var role in new[]{VendorWorkRole.Tea,VendorWorkRole.Market})
            {Interruption(role);Obstruction(role);foreach(int seed in new[]{173,941,8173})LongRun(role,seed);}
            var tea=new VendorWorkRoutine(VendorWorkRole.Tea,173);var market=new VendorWorkRoutine(VendorWorkRole.Market,941);
            int different=0;var teaRestDurations=new HashSet<int>();float previous=0;
            for(int i=0;i<6000;i++)
            {
                tea.Tick(.05f,false,Clear);market.Tick(.05f,false,Clear);
                if(tea.Task!=market.Task)different++;
                if(tea.Task==VendorWorkTask.Rest)previous=tea.ActionTime;
                else if(previous>1){teaRestDurations.Add((int)(previous*20));previous=0;}
            }
            Check(different>3000,"roles do not perform synchronized repeating actions");
            Check(teaRestDurations.Count>=4,"rest lengths vary across cycles");
            Console.WriteLine("VENDOR_WORK_CHECKS_OK "+checks+" assertions; 72,000 long-run movement samples, interruption and obstruction scenarios. No Unity physics/rendering executed.");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
