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
  const rowGap=240;
  const sideLaneGap=14;
  const minimumScale=.08;
  let scale=1,tx=0,ty=0,drag=false,pointerStart=null,last=null,geometry={},rowMetrics={},sidePadding=basePadding;

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

  function parentBarycenter(node,previousIndex){
    const parents=primaryEdges.filter(edge=>edge.to===node.dataset.id&&previousIndex.has(edge.from));
    if(!parents.length)return Number.POSITIVE_INFINITY;
    return parents.reduce((sum,edge)=>sum+previousIndex.get(edge.from),0)/parents.length;
  }
  function placeVirtualTargets(){
    const levels=Object.keys(groups).map(Number).sort((a,b)=>a-b);
    for(const level of levels){
      if(level===0)continue;
      const previous=groups[level-1]||[];
      const previousIndex=new Map(previous.map((node,index)=>[node.dataset.id,index]));
      const actual=groups[level].filter(node=>node.dataset.virtual!=='true');
      const virtuals=groups[level].filter(node=>node.dataset.virtual==='true')
        .sort((a,b)=>parentBarycenter(a,previousIndex)-parentBarycenter(b,previousIndex));
      const ordered=[...actual];
      virtuals.forEach(virtual=>{
        const wanted=parentBarycenter(virtual,previousIndex);
        let position=ordered.findIndex(node=>parentBarycenter(node,previousIndex)>wanted);
        if(position<0)position=ordered.length;
        ordered.splice(position,0,virtual);
      });
      groups[level]=ordered;
    }
  }
  placeVirtualTargets();

  function isDetour(edge){
    return edge.marker==='loop'||depth[edge.to]!==depth[edge.from]+1;
  }
  function detourEdges(){
    return localEdges.filter(isDetour);
  }
  function distributedLane(candidates,edge,start,end){
    const index=Math.max(0,candidates.indexOf(edge));
    return start+(end-start)*(index+1)/(candidates.length+1);
  }

  function layout(){
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

  function orderedOutgoing(edge){
    return primaryEdges.filter(candidate=>candidate.from===edge.from&&geometry[candidate.to])
      .sort((a,b)=>(geometry[a.to].x+geometry[a.to].w/2)-(geometry[b.to].x+geometry[b.to].w/2));
  }
  function orderedIncoming(edge){
    return primaryEdges.filter(candidate=>candidate.to===edge.to&&geometry[candidate.from])
      .sort((a,b)=>(geometry[a.from].x+geometry[a.from].w/2)-(geometry[b.from].x+geometry[b.from].w/2));
  }
  function distributedPort(rect,index,count){
    return rect.x+rect.w*(index+1)/(count+1);
  }
  function route(edge,index){
    const from=geometry[edge.from],to=geometry[edge.to];
    if(!from||!to)return[];
    if(to.level>from.level){
      const outgoing=orderedOutgoing(edge);
      const incoming=orderedIncoming(edge);
      const startIndex=Math.max(0,outgoing.indexOf(edge));
      const endIndex=Math.max(0,incoming.indexOf(edge));
      const start={x:distributedPort(from,startIndex,Math.max(1,outgoing.length)),y:from.y+from.h};
      const end={x:distributedPort(to,endIndex,Math.max(1,incoming.length)),y:to.y};
      if(to.level===from.level+1&&edge.marker!=='loop'){
        const departureY=rowMetrics[from.level].bottom+24;
        const arrivalY=rowMetrics[to.level].top-24;
        return[start,{x:start.x,y:departureY},{x:end.x,y:arrivalY},end];
      }
      const detours=detourEdges();
      const detourIndex=Math.max(0,detours.indexOf(edge));
      const lane=Math.floor(detourIndex/2);
      const useLeft=true;
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
    const useLeft=true;
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
  function rectOverlaps(a,b,padding=0){
    return a.x<b.x+b.w+padding&&a.x+a.w+padding>b.x&&a.y<b.y+b.h+padding&&a.y+a.h+padding>b.y;
  }
  function pointInRect(point,rect,padding=0){
    return point.x>=rect.x-padding&&point.x<=rect.x+rect.w+padding&&point.y>=rect.y-padding&&point.y<=rect.y+rect.h+padding;
  }
  function segmentHitsRect(a,b,rect,padding=0){
    const expanded={x:rect.x-padding,y:rect.y-padding,w:rect.w+padding*2,h:rect.h+padding*2};
    if(pointInRect(a,expanded)||pointInRect(b,expanded))return true;
    const left=expanded.x,right=expanded.x+expanded.w,top=expanded.y,bottom=expanded.y+expanded.h;
    const edges=[[{x:left,y:top},{x:right,y:top}],[{x:right,y:top},{x:right,y:bottom}],[{x:right,y:bottom},{x:left,y:bottom}],[{x:left,y:bottom},{x:left,y:top}]];
    function cross(p,q,r){return(q.x-p.x)*(r.y-p.y)-(q.y-p.y)*(r.x-p.x);}
    function intersects(p1,p2,q1,q2){
      const a1=cross(p1,p2,q1),a2=cross(p1,p2,q2),b1=cross(q1,q2,p1),b2=cross(q1,q2,p2);
      return a1*a2<0&&b1*b2<0;
    }
    return edges.some(([c,d])=>intersects(a,b,c,d));
  }
  function middleSegment(points){
    if(points.length>=4)return[points[1],points[2]];
    let best=[points[0],points[1]],length=-1;
    for(let index=0;index<points.length-1;index++){
      const a=points[index],b=points[index+1];
      const candidate=Math.hypot(b.x-a.x,b.y-a.y);
      if(candidate>length){best=[a,b];length=candidate;}
    }
    return best;
  }
  function labelCandidate(segment,fraction,normalOffset,width,height){
    const [a,b]=segment;
    const dx=b.x-a.x,dy=b.y-a.y,length=Math.max(1,Math.hypot(dx,dy));
    const center={x:a.x+dx*fraction,y:a.y+dy*fraction};
    const nx=-dy/length,ny=dx/length;
    center.x+=nx*normalOffset;center.y+=ny*normalOffset;
    return{x:center.x-width/2,y:center.y-height/2,w:width,h:height};
  }
  function labelRectClear(rect,routeItem,routes,placed){
    if(placed.some(other=>rectOverlaps(rect,other,8)))return false;
    const nodeRects=Object.values(geometry).map(node=>({x:node.x,y:node.y,w:node.w,h:node.h}));
    if(nodeRects.some(node=>rectOverlaps(rect,node,10)))return false;
    for(const other of routes){
      if(other===routeItem)continue;
      for(let index=0;index<other.points.length-1;index++){
        if(segmentHitsRect(other.points[index],other.points[index+1],rect,5))return false;
      }
    }
    return true;
  }
  function addLabel(routeItem,routes,placed){
    const value=routeItem.edge.label;
    const lines=labelLines(value);
    const width=Math.min(238,Math.max(82,Math.max(...lines.map(line=>line.length))*6.35+20));
    const height=lines.length*16+8;
    const segment=middleSegment(routeItem.points);
    const siblings=routes.filter(item=>item.edge.label&&item.edge.from===routeItem.edge.from);
    const siblingIndex=Math.max(0,siblings.indexOf(routeItem));
    const preferred=siblings.length<=1?.5:.22+.56*siblingIndex/Math.max(1,siblings.length-1);
    const fractions=[preferred,.5,.36,.64,.26,.74,.16,.84];
    const offsetStep=height/2+12;
    const offsets=[0,-offsetStep,offsetStep,-offsetStep*2,offsetStep*2];
    let rect=null;
    outer:for(const fraction of fractions){
      for(const offset of offsets){
        const candidate=labelCandidate(segment,fraction,offset,width,height);
        if(labelRectClear(candidate,routeItem,routes,placed)){rect=candidate;break outer;}
      }
    }
    if(!rect)rect=labelCandidate(segment,preferred,0,width,height);
    placed.push(rect);
    const group=document.createElementNS('http://www.w3.org/2000/svg','g');
    group.setAttribute('class','edge-caption');
    const background=document.createElementNS('http://www.w3.org/2000/svg','rect');
    background.setAttribute('class','edge-label-bg');
    background.setAttribute('x',rect.x);background.setAttribute('y',rect.y);background.setAttribute('width',width);background.setAttribute('height',height);background.setAttribute('rx','9');
    group.append(background);
    const text=document.createElementNS('http://www.w3.org/2000/svg','text');
    text.setAttribute('class','edge-label');text.setAttribute('x',rect.x+width/2);text.setAttribute('y',rect.y+16);text.setAttribute('text-anchor','middle');
    lines.forEach((line,index)=>{
      const tspan=document.createElementNS('http://www.w3.org/2000/svg','tspan');
      tspan.setAttribute('x',rect.x+width/2);tspan.setAttribute('dy',index?'16':'0');tspan.textContent=line;text.append(tspan);
    });
    group.append(text);svg.append(group);
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
    drawMergeMarkers(routes);
    const placed=[];
    routes.forEach(item=>{if(item.edge.label)addLabel(item,routes,placed);});
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
