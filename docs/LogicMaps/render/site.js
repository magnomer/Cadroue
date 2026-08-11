(()=>{
  const root=document.documentElement;
  const saved=localStorage.getItem('lmap-theme');
  if(saved)root.dataset.theme=saved;

  function applyTheme(theme){
    if(theme!=='dark'&&theme!=='light')return;
    root.dataset.theme=theme;
  }
  function broadcastTheme(theme){
    document.querySelectorAll('iframe.nav-frame').forEach(frame=>{
      try{frame.contentWindow?.postMessage({type:'lmap-theme',theme},'*');}catch{}
    });
  }
  window.addEventListener('message',event=>{
    if(event.data?.type==='lmap-theme')applyTheme(event.data.theme);
  });
  document.querySelectorAll('iframe.nav-frame').forEach(frame=>frame.addEventListener('load',()=>{
    const theme=root.dataset.theme||localStorage.getItem('lmap-theme');
    if(theme)broadcastTheme(theme);
  }));
  document.querySelectorAll('[data-theme-toggle]').forEach(button=>button.onclick=()=>{
    const theme=root.dataset.theme==='dark'?'light':'dark';
    applyTheme(theme);
    localStorage.setItem('lmap-theme',theme);
    broadcastTheme(theme);
  });

  document.querySelectorAll('[data-filter-input]').forEach(input=>input.addEventListener('input',()=>{
    const query=input.value.toLowerCase().trim();
    let shown=0;
    document.querySelectorAll('[data-search]').forEach(item=>{
      const visible=!query||item.dataset.search.includes(query);
      item.style.display=visible?'':'none';
      if(visible)shown++;
    });
    document.querySelectorAll('[data-filter-empty]').forEach(empty=>empty.style.display=shown?'none':'block');
  }));

  const canvas=document.querySelector('.canvas');
  if(!canvas)return;
  const world=canvas.querySelector('.world');
  const nodeLayer=canvas.querySelector('.nodes');
  const nodes=[...canvas.querySelectorAll('.node')];
  const svg=canvas.querySelector('.edges');
  const edges=JSON.parse(canvas.dataset.edges||'[]');
  const declaredEntries=JSON.parse(canvas.dataset.entries||'[]');
  const entrySet=new Set(declaredEntries);
  const byId=Object.fromEntries(nodes.map(node=>[node.dataset.id,node]));
  const localEdges=edges.filter(edge=>byId[edge.from]&&byId[edge.to]);
  const primaryEdges=localEdges.filter(edge=>edge.marker!=='loop');
  const depth={};
  const groups={};
  const basePadding=90;
  const columnGap=82;
  const rowGap=170;
  const sideLaneGap=14;
  const minimumScale=.08;
  let scale=1,tx=0,ty=0,drag=false,pointerStart=null,last=null,geometry={},rowMetrics={},sidePadding=basePadding,adjacentPlans=new Map();

  function visit(id,seen=new Set()){
    if(depth[id]!=null)return depth[id];
    if(entrySet.has(id)){depth[id]=0;return 0;}
    if(seen.has(id))return 0;
    seen.add(id);
    const parents=primaryEdges.filter(edge=>edge.to===id&&!entrySet.has(edge.to));
    depth[id]=parents.length?Math.max(...parents.map(edge=>visit(edge.from,new Set(seen))+1)):0;
    return depth[id];
  }
  nodes.forEach(node=>visit(node.dataset.id));
  nodes.forEach(node=>(groups[depth[node.dataset.id]]??=[]).push(node));

  function average(values,fallback){return values.length?values.reduce((sum,value)=>sum+value,0)/values.length:fallback;}
  function markerBias(marker){return marker==='left'?-.42:marker==='right'?.42:0;}
  function positionsFor(level){
    return Object.fromEntries((groups[level]||[]).map((node,index)=>[node.dataset.id,index]));
  }
  function adjacentCrossings(level){
    const left=groups[level]||[],right=groups[level+1]||[];
    if(!left.length||!right.length)return 0;
    const leftPos=positionsFor(level),rightPos=positionsFor(level+1);
    const levelEdges=primaryEdges.filter(edge=>depth[edge.from]===level&&depth[edge.to]===level+1);
    let total=0;
    for(let i=0;i<levelEdges.length;i++){
      for(let j=i+1;j<levelEdges.length;j++){
        const a=levelEdges[i],b=levelEdges[j];
        const af=leftPos[a.from],bf=leftPos[b.from],at=rightPos[a.to],bt=rightPos[b.to];
        if(af==null||bf==null||at==null||bt==null)continue;
        if(af===bf||at===bt)continue;
        if((af-bf)*(at-bt)<0)total++;
      }
    }
    return total;
  }
  function crossingsAround(level){
    return adjacentCrossings(level-1)+adjacentCrossings(level);
  }
  function improveLocalOrder(level){
    const row=groups[level]||[];
    if(row.length<2)return false;
    let improved=false;
    let changed=true;
    while(changed){
      changed=false;
      for(let index=0;index<row.length-1;index++){
        const before=crossingsAround(level);
        [row[index],row[index+1]]=[row[index+1],row[index]];
        const after=crossingsAround(level);
        if(after<before){changed=true;improved=true;continue;}
        [row[index],row[index+1]]=[row[index+1],row[index]];
      }
    }
    return improved;
  }
  function reduceCrossings(){
    const levels=Object.keys(groups).map(Number).sort((a,b)=>a-b);
    for(let pass=0;pass<8;pass++){
      for(const level of levels.slice(1)){
        const positions=positionsFor(level-1);
        const original=positionsFor(level);
        groups[level].sort((a,b)=>{
          const ap=primaryEdges.filter(edge=>edge.to===a.dataset.id&&positions[edge.from]!=null).map(edge=>positions[edge.from]+markerBias(edge.marker));
          const bp=primaryEdges.filter(edge=>edge.to===b.dataset.id&&positions[edge.from]!=null).map(edge=>positions[edge.from]+markerBias(edge.marker));
          return average(ap,original[a.dataset.id])-average(bp,original[b.dataset.id]);
        });
      }
      for(const level of levels.slice(0,-1).reverse()){
        const positions=positionsFor(level+1);
        const original=positionsFor(level);
        groups[level].sort((a,b)=>{
          const ac=primaryEdges.filter(edge=>edge.from===a.dataset.id&&positions[edge.to]!=null).map(edge=>positions[edge.to]-markerBias(edge.marker));
          const bc=primaryEdges.filter(edge=>edge.from===b.dataset.id&&positions[edge.to]!=null).map(edge=>positions[edge.to]-markerBias(edge.marker));
          return average(ac,original[a.dataset.id])-average(bc,original[b.dataset.id]);
        });
      }
    }
    for(let sweep=0;sweep<12;sweep++){
      let changed=false;
      for(const level of levels){
        changed=improveLocalOrder(level)||changed;
      }
      if(!changed)break;
    }
  }

  function isDetour(edge){
    return edge.marker==='left'||edge.marker==='right'||edge.marker==='loop'||depth[edge.to]!==depth[edge.from]+1;
  }
  function detourEdges(){
    return localEdges.filter(isDetour);
  }
  function distributedLane(candidates,edge,start,end){
    const index=Math.max(0,candidates.indexOf(edge));
    return start+(end-start)*(index+1)/(candidates.length+1);
  }
  function adjacentPlan(fromLevel,toLevel){
    const key=`${fromLevel}->${toLevel}`;
    if(adjacentPlans.has(key))return adjacentPlans.get(key);
    const pairEdges=primaryEdges.filter(edge=>!edge.marker&&geometry[edge.from]?.level===fromLevel&&geometry[edge.to]?.level===toLevel&&toLevel===fromLevel+1);
    const grouped=new Map();
    pairEdges.forEach(edge=>{
      const groupKey=edge.to;
      if(!grouped.has(groupKey))grouped.set(groupKey,[]);
      grouped.get(groupKey).push(edge);
    });
    const corridorTop=rowMetrics[fromLevel].bottom;
    const corridorBottom=rowMetrics[toLevel].top;
    const bundles=[...grouped.entries()].map(([target,bundleEdges])=>{
      const targetGeom=geometry[target];
      const targetCenter=targetGeom.x+targetGeom.w/2;
      const sourceCenters=bundleEdges.map(item=>geometry[item.from].x+geometry[item.from].w/2);
      return {target,edges:bundleEdges,targetCenter,sourceCenter:average(sourceCenters,targetCenter),sortKey:average(sourceCenters,targetCenter)};
    }).sort((a,b)=>a.sortKey-b.sortKey||a.targetCenter-b.targetCenter);
    const edgeToBundle=new Map();
    bundles.forEach((bundle,index)=>{
      bundle.channel=corridorTop+(corridorBottom-corridorTop)*(index+1)/(bundles.length+1);
      bundle.edges.sort((a,b)=>(geometry[a.from].x+geometry[a.from].w/2)-(geometry[b.from].x+geometry[b.from].w/2));
      bundle.edges.forEach(edge=>edgeToBundle.set(edge,bundle));
    });
    const plan={bundles,edgeToBundle};
    adjacentPlans.set(key,plan);
    return plan;
  }

  function layout(){
    adjacentPlans=new Map();
    reduceCrossings();
    const levels=Object.keys(groups).map(Number).sort((a,b)=>a-b);
    const nodeWidth=Math.max(...nodes.map(node=>node.offsetWidth),340);
    const widest=Math.max(...levels.map(level=>groups[level].length),1);
    const contentWidth=widest*nodeWidth+(widest-1)*columnGap;
    const detourCount=detourEdges().length;
    sidePadding=Math.max(basePadding,48+Math.ceil(detourCount/2)*sideLaneGap);
    const worldWidth=contentWidth+sidePadding*2;
    let y=basePadding;
    geometry={};
    rowMetrics={};
    for(const level of levels){
      const row=groups[level];
      const rowWidth=row.length*nodeWidth+(row.length-1)*columnGap;
      const startX=sidePadding+(contentWidth-rowWidth)/2;
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
    const worldHeight=Math.max(canvas.clientHeight,y-rowGap+basePadding);
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
      if(to.level===from.level+1&&!edge.marker){
        const plan=adjacentPlan(from.level,to.level);
        const bundle=plan.edgeToBundle.get(edge);
        const channel=bundle?.channel ?? average([rowMetrics[from.level].bottom,rowMetrics[to.level].top],rowMetrics[from.level].bottom+24);
        return[start,{x:start.x,y:channel},{x:end.x,y:channel},end];
      }
      const detours=detourEdges();
      const detourIndex=Math.max(0,detours.indexOf(edge));
      const lane=Math.floor(detourIndex/2);
      const useLeft=edge.marker==='left'?true:edge.marker==='right'?false:detourIndex%2===0;
      const outerX=useLeft?sidePadding-28-lane*sideLaneGap:world.offsetWidth-sidePadding+28+lane*sideLaneGap;
      const departures=detours.filter(candidate=>depth[candidate.from]===from.level);
      const arrivals=detours.filter(candidate=>depth[candidate.to]===to.level);
      const firstY=distributedLane(departures,edge,rowMetrics[from.level].bottom+14,rowMetrics[from.level].bottom+72);
      const lastY=distributedLane(arrivals,edge,rowMetrics[to.level].top-72,rowMetrics[to.level].top-14);
      return[start,{x:start.x,y:firstY},{x:outerX,y:firstY},{x:outerX,y:lastY},{x:end.x,y:lastY},end];
    }
    const start={x:from.x+from.w/2,y:from.y};
    const end={x:to.x+to.w/2,y:to.y};
    const detours=detourEdges();
    const detourIndex=Math.max(0,detours.indexOf(edge));
    const lane=Math.floor(detourIndex/2);
    const useLeft=edge.marker==='left'?true:edge.marker==='right'?false:detourIndex%2===0;
    const outerX=useLeft?sidePadding-28-lane*sideLaneGap:world.offsetWidth-sidePadding+28+lane*sideLaneGap;
    const departures=detours.filter(candidate=>depth[candidate.from]===from.level);
    const arrivals=detours.filter(candidate=>depth[candidate.to]===to.level);
    const startY=distributedLane(departures,edge,Math.max(24,rowMetrics[from.level].top-72),Math.max(30,rowMetrics[from.level].top-14));
    const endY=distributedLane(arrivals,edge,Math.max(24,rowMetrics[to.level].top-72),Math.max(30,rowMetrics[to.level].top-14));
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

  function segments(points){
    const result=[];
    for(let index=0;index<points.length-1;index++){
      const a=points[index],b=points[index+1];
      if(a.x===b.x&&a.y===b.y)continue;
      result.push({a,b,horizontal:Math.abs(a.y-b.y)<.1,vertical:Math.abs(a.x-b.x)<.1});
    }
    return result;
  }
  function inside(value,a,b,margin=8){
    return value>Math.min(a,b)+margin&&value<Math.max(a,b)-margin;
  }
  function crossing(horizontal,vertical){
    const x=vertical.a.x,y=horizontal.a.y;
    if(!inside(x,horizontal.a.x,horizontal.b.x)||!inside(y,vertical.a.y,vertical.b.y))return null;
    return{x,y,orientation:'horizontal'};
  }
  function crossingBridges(routes){
    const found=[];
    const seen=new Set();
    for(let left=0;left<routes.length;left++){
      for(let right=left+1;right<routes.length;right++){
        const leftSegments=segments(routes[left].points);
        const rightSegments=segments(routes[right].points);
        for(const a of leftSegments){
          for(const b of rightSegments){
            let point=null,overRoute=null;
            if(a.horizontal&&b.vertical){point=crossing(a,b);overRoute=left;}
            else if(a.vertical&&b.horizontal){point=crossing(b,a);overRoute=right;}
            if(!point)continue;
            const key=`${Math.round(point.x)}:${Math.round(point.y)}:${overRoute}`;
            if(seen.has(key))continue;
            seen.add(key);
            found.push({...point,overRoute});
          }
        }
      }
    }
    return found;
  }
  function drawBridge(point,routes){
    const radius=8;
    const mask=document.createElementNS('http://www.w3.org/2000/svg','path');
    mask.setAttribute('class','edge-bridge-mask');
    mask.setAttribute('d',`M${point.x-radius-2},${point.y} L${point.x+radius+2},${point.y}`);
    svg.append(mask);
    const bridge=document.createElementNS('http://www.w3.org/2000/svg','path');
    bridge.setAttribute('class','edge edge-bridge');
    bridge.setAttribute('d',`M${point.x-radius},${point.y} C${point.x-radius*.45},${point.y-radius} ${point.x+radius*.45},${point.y-radius} ${point.x+radius},${point.y}`);
    const owner=routes[point.overRoute]?.edge;
    if(owner){bridge.dataset.from=owner.from;bridge.dataset.to=owner.to;}
    svg.append(bridge);
  }
  function drawMergeMarkers(routes){
    const incoming=new Map();
    routes.forEach(route=>(incoming.get(route.edge.to)??incoming.set(route.edge.to,[]).get(route.edge.to)).push(route));
    incoming.forEach((routesForTarget,target)=>{
      if(routesForTarget.length<2||byId[target]?.classList.contains('junction'))return;
      const end=routesForTarget[0].points.at(-1);
      if(!end)return;
      const dot=document.createElementNS('http://www.w3.org/2000/svg','circle');
      dot.setAttribute('class','merge-junction');
      dot.setAttribute('cx',end.x);dot.setAttribute('cy',end.y-10);dot.setAttribute('r','4.5');
      svg.append(dot);
    });
  }

  function draw(){
    if(!Object.keys(geometry).length)return;
    svg.setAttribute('width',world.offsetWidth);svg.setAttribute('height',world.offsetHeight);svg.replaceChildren();
    const defs=document.createElementNS('http://www.w3.org/2000/svg','defs');
    const marker=document.createElementNS('http://www.w3.org/2000/svg','marker');
    marker.setAttribute('id','arrow');marker.setAttribute('markerWidth','8');marker.setAttribute('markerHeight','8');marker.setAttribute('refX','7');marker.setAttribute('refY','4');marker.setAttribute('orient','auto');
    const arrow=document.createElementNS('http://www.w3.org/2000/svg','path');
    arrow.setAttribute('d','M0 0L8 4L0 8Z');arrow.setAttribute('fill','var(--edge)');marker.append(arrow);defs.append(marker);svg.append(defs);

    const routes=edges.map((edge,index)=>({edge,index,points:route(edge,index)})).filter(item=>item.points.length);
    routes.forEach(item=>{
      const path=document.createElementNS('http://www.w3.org/2000/svg','path');
      path.setAttribute('d',pathData(item.points));path.setAttribute('class','edge');path.setAttribute('marker-end','url(#arrow)');path.dataset.from=item.edge.from;path.dataset.to=item.edge.to;svg.append(path);
    });
    crossingBridges(routes).forEach(point=>drawBridge(point,routes));
    drawMergeMarkers(routes);
    routes.forEach(item=>{if(item.edge.label)addLabel(item.points,item.edge.label);});
  }

  function apply(){world.style.transform=`translate(${tx}px,${ty}px) scale(${scale})`;}
  function readable(){scale=1;tx=world.offsetWidth<=canvas.clientWidth?(canvas.clientWidth-world.offsetWidth)/2:18;ty=18;apply();}
  function fit(){
    const width=world.offsetWidth,height=world.offsetHeight;
    scale=Math.max(minimumScale,Math.min(1,(canvas.clientWidth-24)/width,(canvas.clientHeight-24)/height));
    tx=(canvas.clientWidth-width*scale)/2;ty=12;apply();
  }
  requestAnimationFrame(()=>requestAnimationFrame(()=>{layout();readable();}));
  new ResizeObserver(()=>{layout();apply();}).observe(canvas);
  canvas.addEventListener('wheel',event=>{
    event.preventDefault();
    tx-=event.deltaX;
    ty-=event.deltaY;
    apply();
  },{passive:false});
  canvas.addEventListener('selectstart',event=>{
    if(!event.target.closest('.node'))event.preventDefault();
  });
  canvas.onpointerdown=event=>{
    if(event.button!==0||event.target.closest('.node'))return;
    event.preventDefault();
    pointerStart=[event.clientX,event.clientY];
    last=pointerStart;
    canvas.classList.add('is-panning');
    canvas.setPointerCapture(event.pointerId);
  };
  canvas.onpointermove=event=>{
    if(!pointerStart||!last)return;
    const deltaX=event.clientX-pointerStart[0],deltaY=event.clientY-pointerStart[1];
    if(!drag&&Math.hypot(deltaX,deltaY)<5)return;
    drag=true;
    tx+=event.clientX-last[0];ty+=event.clientY-last[1];last=[event.clientX,event.clientY];apply();
  };
  function finishPointer(event){
    if(event.pointerId!=null&&canvas.hasPointerCapture(event.pointerId))canvas.releasePointerCapture(event.pointerId);
    canvas.classList.remove('is-panning');
    drag=false;pointerStart=null;last=null;
  }
  canvas.onpointerup=finishPointer;
  canvas.onpointercancel=finishPointer;
  document.querySelector('[data-fit]')?.addEventListener('click',fit);
  document.querySelector('[data-in]')?.addEventListener('click',()=>{scale=Math.min(1.8,scale+.1);apply();});
  document.querySelector('[data-out]')?.addEventListener('click',()=>{scale=Math.max(minimumScale,scale-.1);apply();});
  nodes.forEach(node=>node.addEventListener('focus',()=>{
    nodes.forEach(item=>item.classList.remove('focused'));node.classList.add('focused');
    svg.querySelectorAll('.edge').forEach(path=>path.classList.toggle('active',path.dataset.from===node.dataset.id||path.dataset.to===node.dataset.id));
  }));
})();
