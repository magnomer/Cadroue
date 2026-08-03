(()=>{
  const root=document.documentElement;
  const saved=localStorage.getItem('lmap-theme');
  if(saved)root.dataset.theme=saved;
  document.querySelectorAll('[data-theme-toggle]').forEach(button=>button.onclick=()=>{
    const theme=root.dataset.theme==='dark'?'light':'dark';
    root.dataset.theme=theme;
    localStorage.setItem('lmap-theme',theme);
  });

  const canvas=document.querySelector('.canvas');
  if(!canvas)return;
  const world=canvas.querySelector('.world');
  const nodeLayer=canvas.querySelector('.nodes');
  const nodes=[...canvas.querySelectorAll('.node')];
  const svg=canvas.querySelector('.edges');
  const edges=JSON.parse(canvas.dataset.edges||'[]');
  const byId=Object.fromEntries(nodes.map(node=>[node.dataset.id,node]));
  const localEdges=edges.filter(edge=>byId[edge.from]&&byId[edge.to]);
  const depth={};
  const groups={};
  const padding=90;
  const columnGap=82;
  const rowGap=170;
  let scale=1,tx=0,ty=0,drag=false,last=null,geometry={},rowMetrics={};

  function visit(id,seen=new Set()){
    if(depth[id]!=null)return depth[id];
    if(seen.has(id))return 0;
    seen.add(id);
    const parents=localEdges.filter(edge=>edge.to===id);
    depth[id]=parents.length?Math.max(...parents.map(edge=>visit(edge.from,new Set(seen))+1)):0;
    return depth[id];
  }
  nodes.forEach(node=>visit(node.dataset.id));
  nodes.forEach(node=>(groups[depth[node.dataset.id]]??=[]).push(node));

  function average(values,fallback){return values.length?values.reduce((sum,value)=>sum+value,0)/values.length:fallback;}
  function reduceCrossings(){
    const levels=Object.keys(groups).map(Number).sort((a,b)=>a-b);
    for(let pass=0;pass<4;pass++){
      for(const level of levels.slice(1)){
        const positions=Object.fromEntries((groups[level-1]||[]).map((node,index)=>[node.dataset.id,index]));
        const original=Object.fromEntries(groups[level].map((node,index)=>[node.dataset.id,index]));
        groups[level].sort((a,b)=>{
          const ap=localEdges.filter(edge=>edge.to===a.dataset.id&&positions[edge.from]!=null).map(edge=>positions[edge.from]);
          const bp=localEdges.filter(edge=>edge.to===b.dataset.id&&positions[edge.from]!=null).map(edge=>positions[edge.from]);
          return average(ap,original[a.dataset.id])-average(bp,original[b.dataset.id]);
        });
      }
      for(const level of levels.slice(0,-1).reverse()){
        const positions=Object.fromEntries((groups[level+1]||[]).map((node,index)=>[node.dataset.id,index]));
        const original=Object.fromEntries(groups[level].map((node,index)=>[node.dataset.id,index]));
        groups[level].sort((a,b)=>{
          const ac=localEdges.filter(edge=>edge.from===a.dataset.id&&positions[edge.to]!=null).map(edge=>positions[edge.to]);
          const bc=localEdges.filter(edge=>edge.from===b.dataset.id&&positions[edge.to]!=null).map(edge=>positions[edge.to]);
          return average(ac,original[a.dataset.id])-average(bc,original[b.dataset.id]);
        });
      }
    }
  }

  function layout(){
    reduceCrossings();
    const levels=Object.keys(groups).map(Number).sort((a,b)=>a-b);
    const nodeWidth=Math.max(...nodes.map(node=>node.offsetWidth),340);
    const widest=Math.max(...levels.map(level=>groups[level].length),1);
    const worldWidth=widest*nodeWidth+(widest-1)*columnGap+padding*2;
    let y=padding;
    geometry={};
    rowMetrics={};
    for(const level of levels){
      const row=groups[level];
      const rowWidth=row.length*nodeWidth+(row.length-1)*columnGap;
      const startX=(worldWidth-rowWidth)/2;
      let rowHeight=0;
      row.forEach((node,index)=>{
        const x=startX+index*(nodeWidth+columnGap);
        node.style.left=`${x}px`;
        node.style.top=`${y}px`;
        rowHeight=Math.max(rowHeight,node.offsetHeight);
        geometry[node.dataset.id]={x,y,w:node.offsetWidth,h:node.offsetHeight,level};
      });
      rowMetrics[level]={top:y,bottom:y+rowHeight};
      y+=rowHeight+rowGap;
    }
    const worldHeight=Math.max(canvas.clientHeight,y-rowGap+padding);
    world.style.width=`${worldWidth}px`;
    world.style.height=`${worldHeight}px`;
    nodeLayer.style.width=`${worldWidth}px`;
    nodeLayer.style.height=`${worldHeight}px`;
    draw();
  }

  function route(edge,index){
    const from=geometry[edge.from],to=geometry[edge.to];
    if(!from||!to)return[];
    if(to.level>from.level){
      const start={x:from.x+from.w/2,y:from.y+from.h};
      const end={x:to.x+to.w/2,y:to.y};
      if(to.level===from.level+1){
        const peers=localEdges.filter(candidate=>geometry[candidate.from]?.level===from.level&&geometry[candidate.to]?.level===to.level);
        const lane=Math.max(0,peers.indexOf(edge));
        const corridorTop=rowMetrics[from.level].bottom;
        const corridorBottom=rowMetrics[to.level].top;
        const channel=corridorTop+(corridorBottom-corridorTop)*(lane+1)/(peers.length+1);
        return[start,{x:start.x,y:channel},{x:end.x,y:channel},end];
      }
      const outerX=(start.x+end.x)<world.offsetWidth?32:world.offsetWidth-32;
      const firstY=rowMetrics[from.level].bottom+28+(index%3)*12;
      const lastY=rowMetrics[to.level].top-28-(index%3)*12;
      return[start,{x:start.x,y:firstY},{x:outerX,y:firstY},{x:outerX,y:lastY},{x:end.x,y:lastY},end];
    }
    const start={x:from.x+from.w/2,y:from.y};
    const end={x:to.x+to.w/2,y:to.y};
    const outerX=(start.x+end.x)<world.offsetWidth?32:world.offsetWidth-32;
    const startY=Math.max(24,rowMetrics[from.level].top-30-(index%3)*12);
    const endY=Math.max(24,rowMetrics[to.level].top-30-(index%3)*12);
    return[start,{x:start.x,y:startY},{x:outerX,y:startY},{x:outerX,y:endY},{x:end.x,y:endY},end];
  }
  function pathData(points){return points.map((point,index)=>`${index?'L':'M'}${point.x},${point.y}`).join(' ');}
  function labelLines(value){
    const lines=[];
    let line='';
    for(const word of value.split(/\s+/)){
      const candidate=line?`${line} ${word}`:word;
      if(candidate.length>31&&line){lines.push(line);line=word;}else line=candidate;
    }
    if(line)lines.push(line);
    return lines;
  }
  function addLabel(points,value){
    const lines=labelLines(value);
    let best={horizontal:false,length:-1,a:points[0],b:points[1]};
    for(let index=0;index<points.length-1;index++){
      const a=points[index],b=points[index+1];
      const horizontal=Math.abs(a.y-b.y)<.1;
      const length=horizontal?Math.abs(a.x-b.x):0;
      if(horizontal&&(!best.horizontal||length>best.length))best={horizontal:true,length,a,b};
    }
    const width=Math.min(238,Math.max(82,Math.max(...lines.map(line=>line.length))*6.35+20));
    const height=lines.length*16+8;
    const x=(best.a.x+best.b.x)/2-width/2;
    const y=(best.a.y+best.b.y)/2-height/2;
    const group=document.createElementNS('http://www.w3.org/2000/svg','g');
    group.setAttribute('class','edge-caption');
    const rect=document.createElementNS('http://www.w3.org/2000/svg','rect');
    rect.setAttribute('class','edge-label-bg');
    rect.setAttribute('x',x);rect.setAttribute('y',y);rect.setAttribute('width',width);rect.setAttribute('height',height);rect.setAttribute('rx','9');
    group.append(rect);
    const text=document.createElementNS('http://www.w3.org/2000/svg','text');
    text.setAttribute('class','edge-label');text.setAttribute('x',x+width/2);text.setAttribute('y',y+16);text.setAttribute('text-anchor','middle');
    lines.forEach((line,index)=>{
      const tspan=document.createElementNS('http://www.w3.org/2000/svg','tspan');
      tspan.setAttribute('x',x+width/2);tspan.setAttribute('dy',index?'16':'0');tspan.textContent=line;text.append(tspan);
    });
    group.append(text);svg.append(group);
  }
  function draw(){
    if(!Object.keys(geometry).length)return;
    svg.setAttribute('width',world.offsetWidth);svg.setAttribute('height',world.offsetHeight);svg.replaceChildren();
    const defs=document.createElementNS('http://www.w3.org/2000/svg','defs');
    const marker=document.createElementNS('http://www.w3.org/2000/svg','marker');
    marker.setAttribute('id','arrow');marker.setAttribute('markerWidth','8');marker.setAttribute('markerHeight','8');marker.setAttribute('refX','7');marker.setAttribute('refY','4');marker.setAttribute('orient','auto');
    const arrow=document.createElementNS('http://www.w3.org/2000/svg','path');
    arrow.setAttribute('d','M0 0L8 4L0 8Z');arrow.setAttribute('fill','var(--edge)');marker.append(arrow);defs.append(marker);svg.append(defs);
    edges.forEach((edge,index)=>{
      const points=route(edge,index);if(!points.length)return;
      const path=document.createElementNS('http://www.w3.org/2000/svg','path');
      path.setAttribute('d',pathData(points));path.setAttribute('class','edge');path.setAttribute('marker-end','url(#arrow)');path.dataset.from=edge.from;path.dataset.to=edge.to;svg.append(path);
      if(edge.label)addLabel(points,edge.label);
    });
  }
  function apply(){world.style.transform=`translate(${tx}px,${ty}px) scale(${scale})`;}
  function readable(){scale=1;tx=world.offsetWidth<=canvas.clientWidth?(canvas.clientWidth-world.offsetWidth)/2:18;ty=18;apply();}
  function fit(){
    const width=world.offsetWidth,height=world.offsetHeight;
    scale=Math.max(.35,Math.min(1,(canvas.clientWidth-24)/width,(canvas.clientHeight-24)/height));
    tx=(canvas.clientWidth-width*scale)/2;ty=12;apply();
  }
  requestAnimationFrame(()=>requestAnimationFrame(()=>{layout();readable();}));
  new ResizeObserver(()=>{layout();apply();}).observe(canvas);
  canvas.addEventListener('wheel',event=>{event.preventDefault();scale=Math.max(.35,Math.min(1.8,scale*(event.deltaY<0?1.1:.9)));apply();},{passive:false});
  canvas.onpointerdown=event=>{if(event.target.closest('.node'))return;drag=true;last=[event.clientX,event.clientY];canvas.setPointerCapture(event.pointerId);};
  canvas.onpointermove=event=>{if(!drag)return;tx+=event.clientX-last[0];ty+=event.clientY-last[1];last=[event.clientX,event.clientY];apply();};
  canvas.onpointerup=()=>drag=false;
  document.querySelector('[data-fit]')?.addEventListener('click',fit);
  document.querySelector('[data-in]')?.addEventListener('click',()=>{scale=Math.min(1.8,scale+.1);apply();});
  document.querySelector('[data-out]')?.addEventListener('click',()=>{scale=Math.max(.35,scale-.1);apply();});
  nodes.forEach(node=>node.addEventListener('focus',()=>{
    nodes.forEach(item=>item.classList.remove('focused'));node.classList.add('focused');
    svg.querySelectorAll('.edge').forEach(path=>path.classList.toggle('active',path.dataset.from===node.dataset.id||path.dataset.to===node.dataset.id));
  }));
})();
