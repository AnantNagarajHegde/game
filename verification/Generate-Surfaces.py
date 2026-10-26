"""Deterministic, seamless source textures for the Paridhwani material kit. No external assets."""
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]/'My project (1)'/'Assets'/'_Paridhwani'/'Art'/'Textures'/'Surfaces'
N=512
rng=np.random.default_rng(1976)
y,x=np.mgrid[0:N,0:N].astype(float)/N
def noise(cells):
    grid=rng.random((cells,cells)); px=x*cells;py=y*cells;ix=px.astype(int);iy=py.astype(int);fx=px-ix;fy=py-iy;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy)
    a=grid[iy%cells,ix%cells]*(1-fx)+grid[iy%cells,(ix+1)%cells]*fx
    b=grid[(iy+1)%cells,ix%cells]*(1-fx)+grid[(iy+1)%cells,(ix+1)%cells]*fx
    return a*(1-fy)+b*fy
broad=noise(4);grain=noise(96);fine=rng.random((N,N));middle=noise(20)
def save(name,value):
    value=np.clip(value,0.16,.98)
    rgb=np.repeat((value[...,None]*255).astype(np.uint8),3,axis=2)
    Image.fromarray(rgb).save(ROOT/(name+'.png'),optimize=True)
ROOT.mkdir(parents=True,exist_ok=True)
ruts=np.exp(-((np.sin(y*np.pi*12+.9*np.sin(x*np.pi*4)))/.16)**2)
mud=.73+.24*(broad-.5)+.13*(middle-.5)+.07*(fine-.5)-np.clip((noise(7)-.65)*.5,0,.12)
for _ in range(70):
    px,py=rng.random(2); radius=rng.uniform(.002,.012)
    dx=np.minimum(abs(x-px),1-abs(x-px));dy=np.minimum(abs(y-py),1-abs(y-py))
    mud+=np.exp(-(dx*dx+dy*dy)/(radius*radius)) * rng.uniform(-.08,.10)
save('WetEarth',mud)
points=rng.random((20,2));dist=[]
for px,py in points:
    dx=np.minimum(abs(x-px),1-abs(x-px));dy=np.minimum(abs(y-py),1-abs(y-py));dist.append(dx*dx+dy*dy)
d=np.sort(np.stack(dist),axis=0);seam=np.exp(-(d[1]-d[0])*1100)
save('WeatheredStone',.79+.14*(middle-.5)+.12*(broad-.5)+.06*(fine-.5)-.19*seam)
crack=np.exp(-((np.sin(x*np.pi*6+.4*np.sin(y*np.pi*6)))/.025)**2)
save('LimePlaster',.83+.13*(broad-.5)+.11*(grain-.5)+.055*(fine-.5)-.06*crack)
save('ClayTile',.81+.10*(middle-.5)+.05*(fine-.5)-.065*(.5+.5*np.cos(x*np.pi*40)))
wood=.8+.09*(broad-.5)+.09*np.sin(x*np.pi*80+.9*np.sin(y*np.pi*4)+2*middle)+.05*(fine-.5)
save('Timber',wood)
save('CottonWeave',.85+.045*np.sin(x*np.pi*256)*np.sin(y*np.pi*256)+.07*(grain-.5)+.035*(fine-.5))
save('Bark',.75+.13*np.sin(x*np.pi*46+middle*5)+.12*(broad-.5)+.04*(fine-.5))
print('Generated',len(list(ROOT.glob('*.png'))),'512px periodic surface textures')
