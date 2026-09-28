#!/usr/bin/env python3
"""Original deterministic chiptune score and game cues; no external samples."""
import math, random, wave, struct
from pathlib import Path
RATE=22050
OUT=Path(__file__).resolve().parent.parent/'Assets/ClashOfCities/Resources/Audio'
OUT.mkdir(parents=True,exist_ok=True)
def freq(note): return 440*2**((note-69)/12)
def tone(buf,start,duration,note,volume=.1,kind='pulse',end=None):
    first=int(start*RATE);n=int(duration*RATE);phase=0
    for j in range(n):
        i=first+j
        if i>=len(buf):break
        t=j/RATE;u=j/max(1,n-1);f=freq(note)*(1 if end is None else 2**((end-note)*u/12))
        phase=(phase+f/RATE)%1
        if kind=='triangle':v=1-4*abs(phase-.5)
        elif kind=='sine':v=math.sin(phase*math.tau)
        else:
            # Band-limited pulse voice, rounded by harmonics instead of hard edges.
            v=sum(math.sin(math.tau*phase*k)/k for k in (1,3,5,7) if k*f<RATE*.45)*.7
        env=min(1,t/.006)*min(1,(duration-t)/.035)
        buf[i]+=volume*v*max(0,env)
def noise(buf,start,duration,volume,seed):
    rng=random.Random(seed);prev=0
    for j in range(int(duration*RATE)):
        i=int(start*RATE)+j
        if i>=len(buf):break
        prev=.35*prev+.65*rng.uniform(-1,1)
        buf[i]+=prev*volume*(1-j/(duration*RATE))**2*min(1,j/60)
def save(name,buf):
    peak=max(abs(x) for x in buf) or 1;gain=min(1,.85/peak)
    pcm=[round(max(-.99,min(.99,x*gain))*32767) for x in buf]
    with wave.open(str(OUT/(name+'.wav')),'wb') as w:
        w.setparams((1,2,RATE,0,'NONE','not compressed'));w.writeframes(struct.pack('<'+'h'*len(pcm),*pcm))
    print(f'{name}: {len(buf)/RATE:.2f}s, peak {max(abs(x) for x in pcm)/32767:.3f}')
def music(name,bpm,battle):
    beat=60/bpm;bars=8;buf=[0.] * round(bars*4*beat*RATE)
    roots=[45,41,48,43,45,41,43,45]
    melodies=[[69,72,76,74,72,69,67,69],[65,69,72,69,76,74,72,69],[67,72,76,79,76,74,72,67],[67,71,74,76,74,71,69,67],
              [76,79,81,79,76,74,72,69],[72,69,65,69,72,76,74,72],[71,74,79,76,74,71,67,71],[72,76,74,72,69,67,69,69]]
    for bar,root in enumerate(roots):
        chord=[root+12,root+15 if bar in (0,4,7) else root+16,root+19]
        for step in range(8):
            start=(bar*4+step*.5)*beat
            tone(buf,start,beat*.43,melodies[bar][step],.115 if battle else .075)
            tone(buf,start,beat*.42,root if step%2==0 else root+12,.14,'triangle')
            if battle or step%2==0:
                tone(buf,start,beat*.21,chord[step%3]+12,.045,'triangle')
            noise(buf,start,.045,.055 if battle else .022,bar*31+step)
        for b in range(4):
            start=(bar*4+b)*beat
            if b%2==0:tone(buf,start,.12,43,.21 if battle else .13,'sine',end=23)
            elif battle:noise(buf,start,.13,.16,bar*17+b)
    save(name,buf)
music('menu',104,False);music('battle',132,True)
def cue(name,duration,notes,kind='pulse',noiselevel=0):
    b=[0.]*int(duration*RATE)
    for start,length,n,v,end in notes:tone(b,start,length,n,v,kind,end)
    if noiselevel:noise(b,0,min(duration,.18),noiselevel,42)
    save(name,b)
cue('ui',.09,[(0,.08,84,.20,88)],'triangle')
cue('attack',.16,[(0,.14,66,.22,45)],noiselevel=.10)
cue('hit',.15,[(0,.13,43,.25,25)],noiselevel=.28)
cue('projectile',.25,[(0,.22,85,.22,54)])
cue('melee',.24,[(0,.20,52,.22,31)],noiselevel=.25)
cue('zone',.45,[(0,.2,60,.16,72),(.12,.22,67,.16,79),(.24,.18,72,.16,84)])
cue('buff',.5,[(0,.15,72,.18,None),(.12,.15,76,.18,None),(.24,.22,79,.18,None)],'triangle')
cue('dodge',.18,[(0,.16,68,.16,90)],noiselevel=.07)
cue('stun',.24,[(0,.11,90,.18,65),(.12,.11,90,.18,65)])
cue('ultimate',.75,[(0,.26,48,.24,72),(.20,.3,67,.20,79),(.40,.3,76,.18,88)],noiselevel=.18)
cue('ko',.75,[(0,.2,64,.24,60),(.2,.2,57,.23,52),(.4,.3,45,.22,33)])
cue('victory',1.1,[(0,.18,72,.19,None),(.18,.18,76,.19,None),(.36,.18,79,.19,None),(.54,.5,84,.18,None)],'triangle')
# Periodic tone uses an integer number of cycles, so the charge loop has no seam.
save('charge',[.13*math.sin(math.tau*220*i/RATE)+.04*math.sin(math.tau*440*i/RATE) for i in range(RATE//5)])
