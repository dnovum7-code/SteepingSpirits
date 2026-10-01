# Portiert 1:1 die Kernlogik von Quests + Inventar + Wirtschaft + Dev-Kit
# (aus Everdawn) plus die neuen 2D-Teile: Spielzeit (GameClock), Daily-Reset,
# Health mit i-Frames und Tag/Nacht-Farbverlauf. Testet die Algorithmen,
# nicht die Unity-Glue. Ausführen:  python3 Tests/logic_test.py

# ---------------- Events (wie QuestEvents / GameplayEvents / InventoryEvents) ----------------
class Ev:
    def __init__(self): self.subs=[]
    def add(self,f): self.subs.append(f)
    def remove(self,f):
        if f in self.subs: self.subs.remove(f)
    def fire(self,*a):
        for f in list(self.subs): f(*a)

OnItemCollected=Ev(); OnQuestCompleted=Ev(); OnObjectiveUpdated=Ev(); OnItemUsed=Ev()

# ---------------- Inventory core (Inventory.cs) ----------------
class ItemData:
    def __init__(self,iid,maxStack=99,usable=False,gold=0):
        self.itemID=iid; self.maxStack=maxStack; self.usable=usable; self.goldValue=gold
    @property
    def stackable(self): return self.maxStack>1

class Stack:
    def __init__(self,item,amt): self.item=item; self.amount=amt
    @property
    def full(self): return self.amount>=self.item.maxStack
    @property
    def space(self): return self.item.maxStack-self.amount
    def add(self,a):
        f=min(a,self.space); self.amount+=f; return a-f
    def remove(self,a):
        t=min(a,self.amount); self.amount-=t; return t

class Inventory:
    def __init__(self,cap=0): self.slots=[]; self.cap=cap
    @property
    def full(self): return self.cap>0 and len(self.slots)>=self.cap
    def add(self,item,amt):
        rem=amt
        if item.stackable:
            for s in self.slots:
                if s.item is item and not s.full:
                    rem=s.add(rem)
                    if rem==0: return 0
        while rem>0 and not self.full:
            take=min(rem,item.maxStack) if item.stackable else 1
            self.slots.append(Stack(item,take)); rem-=take
        return rem
    def remove(self,iid,amt):
        removed=0
        for s in reversed(self.slots):
            if s.item.itemID!=iid: continue
            removed+=s.remove(amt-removed)
            if s.amount==0: self.slots.remove(s)
            if removed>=amt: break
        return removed
    def count(self,iid): return sum(s.amount for s in self.slots if s.item.itemID==iid)
    def has(self,iid,amt=1): return self.count(iid)>=amt

# ---------------- PlayerInventory (manager) ----------------
class PlayerInventory:
    def __init__(self,cap=24): self.inv=Inventory(cap); self.catalog={}
    def register(self,item): self.catalog[item.itemID]=item
    def resolve(self,iid): return self.catalog.get(iid)
    def add(self,item,amt=1):
        left=self.inv.add(item,amt); return left
    def add_id(self,iid,amt=1):
        it=self.resolve(iid)
        return amt if it is None else self.add(it,amt)
    def count(self,iid): return self.inv.count(iid)
    def use(self,iid):
        it=self.resolve(iid)
        if it is None or not it.usable or not self.inv.has(iid): return False
        self.inv.remove(iid,1); OnItemUsed.fire(it); return True

class Wallet:
    def __init__(self,gold=0): self.gold=gold
    def add(self,a):
        if a>0: self.gold+=a

# ---------------- Quests (QuestData / QuestInstance / QuestManager) ----------------
class Objective:
    def __init__(self,oid,stepType,targetID,req): self.oid=oid; self.stepType=stepType; self.targetID=targetID; self.req=req
class Reward:
    def __init__(self,typ,amount=0,itemID=None): self.type=typ; self.amount=amount; self.itemID=itemID
class QuestData:
    def __init__(self,qid,objs,rewards,category="SIDE",repeatable=False):
        self.questID=qid; self.objectives=objs; self.rewards=rewards; self.category=category; self.isRepeatable=repeatable
class QuestInstance:
    def __init__(self,data): self.data=data; self.state="INACTIVE"; self.progress={o.oid:0 for o in data.objectives}
    def obj(self,oid): return next((o for o in self.data.objectives if o.oid==oid),None)
    def add_progress(self,oid,amt):
        o=self.obj(oid)
        if o is None: return 0
        cur=self.progress[oid]; nxt=max(0,min(cur+amt,o.req)); self.progress[oid]=nxt; return nxt-cur
    def obj_complete(self,oid):
        o=self.obj(oid); return o is not None and self.progress[oid]>=o.req
    def all_complete(self): return all(self.progress[o.oid]>=o.req for o in self.data.objectives)

class QuestManager:
    def __init__(self): self.quests={}; self.steps={}
    def register(self,data):
        qi=QuestInstance(data); qi.state="AVAILABLE"; self.quests[data.questID]=qi; return qi
    def get(self,qid): return self.quests.get(qid)
    def start(self,qid):
        qi=self.get(qid)
        if qi is None or qi.state=="ACTIVE": return False
        qi.state="ACTIVE"; self.spawn_steps(qi); return True
    def spawn_steps(self,qi):
        objs=[]
        for o in qi.data.objectives:
            if o.stepType=="Collect": objs.append(CollectStep(self,qi,o))
        self.steps[qi.data.questID]=objs
        for s in objs: s.subscribe()
    def destroy_steps(self,qid):
        for s in self.steps.get(qid,[]): s.unsubscribe()
        self.steps.pop(qid,None)
    def cont(self,qid,oid,amt):
        qi=self.get(qid)
        if qi is None or qi.state!="ACTIVE": return
        if qi.obj(oid) is None: return
        d=qi.add_progress(oid,amt)
        if d==0: return
        OnObjectiveUpdated.fire(qi,oid,qi.progress[oid])
        if qi.all_complete(): self.finish(qid)
    def reset(self,qid):
        qi=self.get(qid)
        if qi is None: return False
        self.destroy_steps(qid); qi.progress={o.oid:0 for o in qi.data.objectives}; qi.state="AVAILABLE"; return True
    def finish(self,qid):
        qi=self.get(qid)
        if qi is None or qi.state!="ACTIVE": return
        qi.state="COMPLETED"; self.destroy_steps(qid); OnQuestCompleted.fire(qi)

# CollectStep (Steps/CollectStep.cs + QuestStepBase)
class CollectStep:
    def __init__(self,mgr,qi,obj): self.mgr=mgr; self.qi=qi; self.obj=obj
    def subscribe(self): OnItemCollected.add(self.on_item)
    def unsubscribe(self): OnItemCollected.remove(self.on_item)
    def on_item(self,iid):
        if iid==self.obj.targetID and not self.qi.obj_complete(self.obj.oid):
            self.mgr.cont(self.qi.data.questID,self.obj.oid,1)

# ---------------- Integration wiring (bridges/collectors) ----------------
class World:
    def __init__(self):
        self.inv=PlayerInventory(); self.wallet=Wallet(); self.mgr=QuestManager()
        # InventoryPickupBridge
        OnItemCollected.add(lambda iid: self.inv.add_id(iid,1))
        # QuestRewardCollector + GoldRewardCollector
        OnQuestCompleted.add(self.on_quest_complete)
        # Demo: Trank benutzen heilt
        self.player_hp=70.0
        OnItemUsed.add(self.on_item_used)
    def on_quest_complete(self,qi):
        for r in qi.data.rewards:
            if r.type=="Item" and r.itemID: self.inv.add_id(r.itemID,max(1,r.amount))
            elif r.type=="Gold" and r.amount>0: self.wallet.add(r.amount)
    def on_item_used(self,item):
        if item.itemID=="potion_small": self.player_hp=min(100.0,self.player_hp+25.0)

# ================= TEST SCENARIO =================
results=[]
def check(name,cond):
    results.append((name,cond)); print(("  PASS  " if cond else "  FAIL  ")+name)

w=World()
coin=ItemData("item_coin",maxStack=999); flower=ItemData("item_flower",maxStack=50)
potion=ItemData("potion_small",maxStack=20,usable=True,gold=15)
for it in (coin,flower,potion): w.inv.register(it)

# zwei Collect-Quests: Münzen (reward Gold 50 + Trank) und Blumen (reward XP 20)
qc=QuestData("Q_coins",[Objective("collect_coins","Collect","item_coin",5)],
             [Reward("Gold",50),Reward("Item",1,"potion_small")])
qf=QuestData("Q_flowers",[Objective("collect_flowers","Collect","item_flower",3)],[Reward("XP",20)])
w.mgr.register(qc); w.mgr.register(qf)

print("\n=== 1) Start beide Quests ===")
w.mgr.start("Q_coins"); w.mgr.start("Q_flowers")
check("beide Quests ACTIVE", w.mgr.get("Q_coins").state=="ACTIVE" and w.mgr.get("Q_flowers").state=="ACTIVE")

print("\n=== 2) Broadcast-Filter: 2 Münzen + 1 Blume ===")
OnItemCollected.fire("item_coin"); OnItemCollected.fire("item_coin"); OnItemCollected.fire("item_flower")
check("Münz-Quest 2/5 (Blume zählt NICHT)", w.mgr.get("Q_coins").progress["collect_coins"]==2)
check("Blumen-Quest 1/3", w.mgr.get("Q_flowers").progress["collect_flowers"]==1)
check("Inventar: 2 Münzen via Bridge", w.inv.count("item_coin")==2)
check("Inventar: 1 Blume via Bridge", w.inv.count("item_flower")==1)

print("\n=== 3) Münz-Quest fertigsammeln (noch 3 Münzen) ===")
for _ in range(3): OnItemCollected.fire("item_coin")
check("Münz-Quest COMPLETED bei 5/5", w.mgr.get("Q_coins").state=="COMPLETED")
check("Reward: +50 Gold im Wallet", w.wallet.gold==50)
check("Reward: Trank im Inventar", w.inv.count("potion_small")==1)
check("Inventar zählt 5 Münzen", w.inv.count("item_coin")==5)

print("\n=== 4) Nach Abschluss weitere Münze -> Quest bleibt fertig, Inventar zählt weiter ===")
OnItemCollected.fire("item_coin")
check("Münz-Quest bleibt COMPLETED (kein Overflow)", w.mgr.get("Q_coins").state=="COMPLETED")
check("Inventar jetzt 6 Münzen", w.inv.count("item_coin")==6)

print("\n=== 5) Blumen-Quest fertig (noch 2 Blumen) ===")
for _ in range(2): OnItemCollected.fire("item_flower")
check("Blumen-Quest COMPLETED bei 3/3", w.mgr.get("Q_flowers").state=="COMPLETED")

print("\n=== 6) Trank benutzen -> heilt Spieler (70 -> 95) ===")
before=w.player_hp; ok=w.inv.use("potion_small")
check("Trank benutzt (return true)", ok)
check("Trank aus Inventar entfernt", w.inv.count("potion_small")==0)
check("Spieler geheilt 70 -> 95", abs(w.player_hp-95.0)<0.001)

print("\n=== 7) Dev-Kit: Gold geben + Item geben + Quest per Cheat abschliessen ===")
w.wallet.add(1000); check("Cheat +1000 Gold -> 1050", w.wallet.gold==1050)
w.inv.add_id("item_flower",10); check("Cheat +10 Blumen (3 gesammelt +10) -> 13", w.inv.count("item_flower")==13)
# ForceComplete-Logik (DevCheatWindow): Quest neu, starten, alle Ziele auffuellen
qx=QuestData("Q_cheat",[Objective("c","Collect","item_x",7)],[Reward("Gold",5)])
w.mgr.register(qx); 
qi=w.mgr.get("Q_cheat")
if qi.state!="ACTIVE": w.mgr.start("Q_cheat")
for o in qi.data.objectives: w.mgr.cont("Q_cheat",o.oid,o.req)
check("Force-Complete -> COMPLETED", w.mgr.get("Q_cheat").state=="COMPLETED")
check("Force-Complete Gold-Reward angekommen (1050 -> 1055)", w.wallet.gold==1055)

print("\n=== 8) Inventar-Stapel & Kapazitaet ===")
small=Inventory(cap=2); dagger=ItemData("dagger",maxStack=1)
left=small.add(dagger,1)+small.add(dagger,1)+small.add(dagger,1)  # 3. passt nicht (cap 2)
check("nicht-stapelbar: 2 Slots belegt", len(small.slots)==2)
check("3. Dolch passt nicht (leftover=1)", left==1)
big=Inventory(); big.add(coin,1000)  # maxStack 999 -> 2 slots
check("stapelbar: 1000 Muenzen -> 2 Slots (999+1)", len(big.slots)==2 and big.count("item_coin")==1000)


# ---------------- GameClock (World/GameClock.cs) ----------------
SEASONS=["Fruehling","Sommer","Herbst","Winter"]
class GameClock:
    TICK=10
    def __init__(self,start_hour=6,end_hour=26,days_per_season=28):
        self.start=start_hour; self.end=end_hour; self.dps=days_per_season
        self.day=1; self.season=0; self.year=1; self.minute=start_hour*60
        self.on_new_day=Ev(); self.on_season=Ev()
    @property
    def hour(self): return (self.minute//60)%24
    def total_days(self): return (self.year-1)*self.dps*4+self.season*self.dps+self.day
    def tick(self):
        self.minute+=self.TICK
        if self.minute>=self.end*60: self.next_day()
    def next_day(self):
        before=self.season
        self.day+=1
        if self.day>self.dps:
            self.day=1
            if self.season==3: self.season=0; self.year+=1
            else: self.season+=1
        self.minute=self.start*60
        self.on_new_day.fire(self)
        if self.season!=before: self.on_season.fire(self)
    def set_time(self,h,m=0):
        t=max(self.start*60,min(h*60+m,self.end*60-self.TICK)); self.minute=t-t%self.TICK

# DailyQuestReset (Quests/Integration/DailyQuestReset.cs)
def daily_reset(mgr):
    ids=[q.data.questID for q in mgr.quests.values()
         if q.data.category=="DAILY" and q.data.isRepeatable and q.state in ("COMPLETED","FAILED","ABANDONED")]
    for i in ids: mgr.reset(i)
    return len(ids)

# Health mit i-Frames (Combat/Health.cs)
class Health:
    def __init__(self,mx,iframes=0.0): self.max=mx; self.cur=mx; self.iframes=iframes; self.until=-1.0; self.died=0
    def dead(self): return self.cur<=0
    def take(self,amt,now):
        if self.dead() or amt<=0 or now<self.until: return
        self.cur=max(0.0,self.cur-amt)
        if self.iframes>0: self.until=now+self.iframes
        if self.dead(): self.died+=1
    def heal(self,amt):
        if self.dead() or amt<=0: return
        self.cur=min(self.max,self.cur+amt)
    def revive(self,frac=1.0): self.cur=max(0.01,min(self.max*frac,self.max))

# DayNightTint.Evaluate (World/DayNightTint.cs) – nur Alpha
KEYS=[(6,.16),(8,0),(16,0),(18,.18),(20,.42),(22,.55),(26,.62)]
def tint_alpha(h):
    if h<=KEYS[0][0]: return KEYS[0][1]
    for i in range(1,len(KEYS)):
        if h<=KEYS[i][0]:
            a,b=KEYS[i-1],KEYS[i]; t=(h-a[0])/(b[0]-a[0]); return a[1]+(b[1]-a[1])*t
    return KEYS[-1][1]

# QuestJsonValidator: questID-Schema
import re
QID=re.compile(r"^Q_(MAIN|SIDE|DAILY|TUTORIAL|EVENT|WORLD|FACTION)_[A-Za-z0-9]+$")

print("\n=== 9) Spielzeit: ein ganzer Tag 6:00 -> 2:00 ===")
clock=GameClock(); days=[]
clock.on_new_day.add(lambda c: days.append(c.total_days()))
ticks=0
while not days: clock.tick(); ticks+=1
check("Tag dauert 120 Ticks (20h a 6 Ticks)", ticks==120)
check("Nach 2:00 -> Tag 2, 6:00", clock.day==2 and clock.hour==6 and days==[2])
clock.set_time(13,37); check("SetTime rundet auf 10 Min (13:30)", clock.minute==13*60+30)
clock.set_time(30); check("SetTime klemmt vor Tagesende (1:50)", clock.minute==26*60-10)

print("\n=== 10) Kalender: Jahreszeiten & Jahr ===")
c2=GameClock(); seasons=[]
c2.on_season.add(lambda c: seasons.append(c.season))
for _ in range(28*4): c2.next_day()
check("4 Jahreszeiten-Wechsel in 112 Tagen", seasons==[1,2,3,0])
check("Jahr 2, Tag 1 Frühling", c2.year==2 and c2.day==1 and c2.season==0)
check("TotalDays fortlaufend (113)", c2.total_days()==113)

print("\n=== 11) Daily-Quest: jeden Morgen wieder verfügbar ===")
qd=QuestData("Q_DAILY_Schleimjagd",[Objective("d","Collect","item_slime",2)],[Reward("Gold",15)],"DAILY",True)
qs=QuestData("Q_SIDE_Einmal",[Objective("s","Collect","item_s",1)],[],"SIDE",True)
w.mgr.register(qd); w.mgr.register(qs)
w.mgr.start("Q_DAILY_Schleimjagd"); w.mgr.start("Q_SIDE_Einmal")
for _ in range(2): OnItemCollected.fire("item_slime")
OnItemCollected.fire("item_s")
check("Daily + Side COMPLETED", w.mgr.get("Q_DAILY_Schleimjagd").state=="COMPLETED" and w.mgr.get("Q_SIDE_Einmal").state=="COMPLETED")
clock.on_new_day.add(lambda c: daily_reset(w.mgr))
clock.next_day()
check("Daily wieder AVAILABLE", w.mgr.get("Q_DAILY_Schleimjagd").state=="AVAILABLE")
check("Daily-Fortschritt 0", w.mgr.get("Q_DAILY_Schleimjagd").progress["d"]==0)
check("Nicht-Daily bleibt COMPLETED", w.mgr.get("Q_SIDE_Einmal").state=="COMPLETED")
check("Alte Quests unberührt (Q_coins COMPLETED)", w.mgr.get("Q_coins").state=="COMPLETED")
w.mgr.start("Q_DAILY_Schleimjagd"); OnItemCollected.fire("item_slime")
check("Daily am neuen Tag wieder spielbar (1/2)", w.mgr.get("Q_DAILY_Schleimjagd").progress["d"]==1)

print("\n=== 12) Health: Herzen, i-Frames, Wiederbeleben ===")
hp=Health(6,iframes=1.0)
hp.take(1,now=0.0); hp.take(1,now=0.5)
check("Zweiter Treffer in i-Frames ignoriert (6 -> 5)", hp.cur==5)
hp.take(1,now=1.2); check("Nach i-Frames wieder Schaden (5 -> 4)", hp.cur==4)
hp.take(99,now=3.0); check("Tod genau einmal gemeldet", hp.dead() and hp.died==1)
hp.take(1,now=9.0); check("Kein Doppel-Tod", hp.died==1)
hp.heal(3); check("Tote heilen nicht", hp.cur==0)
hp.revive(); check("Revive -> volle Herzen", hp.cur==6)

print("\n=== 13) Tag/Nacht-Tönung ===")
check("Mittag klar (alpha 0)", tint_alpha(12)==0)
check("Abendrot 18:00 leicht (0.18)", abs(tint_alpha(18)-.18)<1e-9)
check("Nachts dunkler als abends", tint_alpha(23)>tint_alpha(19)>tint_alpha(12))
check("Interpolation 21:00 zwischen 20 und 22", .42<tint_alpha(21)<.55)

print("\n=== 14) Quest-ID-Schema (JSON-Validator) ===")
check("Q_DAILY_Schleimjagd gültig", bool(QID.match("Q_DAILY_Schleimjagd")))
check("Q_SIDE_Mit_Unterstrich ungültig", not QID.match("Q_SIDE_Mit_Unterstrich"))
check("Q_FARM_Ernte (unbekannte Kategorie) ungültig", not QID.match("Q_FARM_Ernte"))


# ---------------- Jump'n'Run (Platformer/PlatformerController2D.cs) ----------------
import math
P=dict(dt=0.02,G=48.0,JV=15.0,RUN=9.0,AIRACC=70.0,MAXFALL=18.0,APEX=2.5,APEXM=0.5,DASH=21.0,DASHT=0.15,DASHEND=0.5,
       PUMP=22.0,DAMP=0.12,MAXS=24.0,MAXANG=math.radians(110))
def air_step(x,y,vx,vy,inx,hold=True):
    dt=P["dt"]; tgt=inx*P["RUN"]
    if abs(vx)>P["RUN"] and (inx==0 or math.copysign(1,vx)==math.copysign(1,tgt)):
        vx-=math.copysign(min(abs(vx)-P["RUN"],35*0.5*dt),vx)
    else:
        vx+=max(-P["AIRACC"]*dt,min(P["AIRACC"]*dt,tgt-vx))
    g=P["G"]*(P["APEXM"] if abs(vy)<P["APEX"] and hold else 1)
    vy=max(vy-g*dt,-P["MAXFALL"])
    return x+vx*dt,y+vy*dt,vx,vy
def jump(dash=None,t_dash=0.25):
    x=y=0.0;vx=P["RUN"];vy=P["JV"];t=0;top=0
    if dash:
        while t<t_dash: x,y,vx,vy=air_step(x,y,vx,vy,1); t+=P["dt"]
        n=math.hypot(*dash); dx,dy=dash[0]/n,dash[1]/n
        for _ in range(int(P["DASHT"]/P["dt"])): x+=dx*P["DASH"]*P["dt"]; y+=dy*P["DASH"]*P["dt"]
        vx,vy=dx*P["DASH"]*P["DASHEND"],dy*P["DASH"]*P["DASHEND"]
        if dy>0: vy=min(vy,P["JV"]*0.7)
    while y>=-0.01:
        x,y,vx,vy=air_step(x,y,vx,vy,1); top=max(top,y)
    return x,top
def swing(secs,pump=True,L=3.5):
    th=w=amp=0.0
    for _ in range(int(secs/P["dt"])):
        c,s=math.cos(th),math.sin(th)
        inx=(1 if (w*c)>0 or w==0 else -1) if pump else 0
        w+=(-(P["G"]/L)*s+P["PUMP"]*inx*c/L)*P["dt"]; w*=1-P["DAMP"]*P["dt"]
        w=max(-P["MAXS"]/L,min(P["MAXS"]/L,w)); th+=w*P["dt"]
        if abs(th)>P["MAXANG"]: th=math.copysign(P["MAXANG"],th); w=0
        amp=max(amp,abs(th))
    return amp

print("\n=== 15) Jump'n'Run: Sprung, Dash, Schwingen ===")
w0,h0=jump()
check(f"Sprunghöhe ~2.2 ({h0:.2f}) < Stufe 5", 2.0<h0<2.6)
check(f"Sprung allein schafft die 8er-Lücke nicht ({w0:.1f})", w0<8)
check("Sprung + Dash → schafft die 8er-Lücke", jump((1,0))[0]>8)
check("Sprung + Dash ↑ schafft die 5er-Stufe", max(jump((0,1),t)[1] for t in (0.2,0.25,0.3))>5)
check("Schwingen: ohne Pumpen keine Bewegung", swing(5,False)==0)
check("Schwingen: Pumpen baut auf (2s > 1s)", swing(2)>swing(1)>math.radians(20))
check("Schwingen: kein Überschlag (max 110°)", abs(swing(8)-P["MAXANG"])<1e-9)

# Summary
passed=sum(1 for _,c in results if c); total=len(results)
print(f"\n================  {passed}/{total} Checks bestanden  ================")
print("ALLE TESTS GRÜN" if passed==total else "Es gibt Fehlschläge — siehe FAIL oben")
import sys; sys.exit(0 if passed==total else 1)
