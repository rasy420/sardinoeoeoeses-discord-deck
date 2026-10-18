let socket;
let volumeQueue = Promise.resolve();
const busy = new Set();
function send(data) { if(socket && socket.readyState === 1) socket.send(JSON.stringify(data)); }
async function handle(event) {
 if(event.action === 'com.cocoa.discorddeck.volume') {
  if(event.event === 'willAppear') {
   fetch('http://127.0.0.1:47835/status',{headers:{'X-DiscordDeck-Key':window.discordDeckKey}}).then(r=>r.json()).then(d=>send({event:'setTitle',context:event.context,payload:{title:'Sounds\n'+d.volumePercent+'%',target:0}})).catch(()=>{});
  }
  if(event.event === 'dialRotate') {
   const ticks=Number(event.payload && event.payload.ticks);
   if(!Number.isInteger(ticks)||ticks===0) return;
   const delta=Math.max(-100,Math.min(100,ticks));
   volumeQueue=volumeQueue.catch(()=>{}).then(async()=>{
    const abort=new AbortController(), timer=setTimeout(()=>abort.abort(),4000);
    try {
     const r=await fetch('http://127.0.0.1:47835/volume/delta/'+delta,{method:'POST',headers:{'X-DiscordDeck-Key':window.discordDeckKey},signal:abort.signal});
     const d=await r.json();if(!r.ok||!d.ok)throw new Error('volume');
     send({event:'setTitle',context:event.context,payload:{title:'Sounds\n'+d.volumePercent+'%',target:0}});
    } catch(e) {send({event:'showAlert',context:event.context});}finally{clearTimeout(timer);}
   });
   await volumeQueue;
  }
  return;
 }
 const match = /^com\.cocoa\.discorddeck\.(slot(\d+)|stop)$/.exec(event.action || '');
 if(!match) return;
 if(event.event === 'willAppear') { send({event:'setTitle',context:event.context,payload:{title:match[1]==='stop'?'STOPP':'Sound '+match[2],target:0}}); return; }
 if(event.event !== 'keyDown' || busy.has(event.context)) return;
 busy.add(event.context);
 const abort = new AbortController(), timeout = setTimeout(()=>abort.abort(),5000);
 try {
  const path = match[1] === 'stop' ? '/stop' : '/play/'+match[2];
  const result = await fetch('http://127.0.0.1:47835'+path,{method:'POST',headers:{'X-DiscordDeck-Key':window.discordDeckKey},signal:abort.signal});
  const data = await result.json();
  if(!result.ok || !data.ok) throw new Error(data.error || 'App nicht erreichbar');
  send({event:'showOk',context:event.context});
 } catch(error) { send({event:'showAlert',context:event.context}); }
 finally { clearTimeout(timeout); busy.delete(event.context); }
}
function connectElgatoStreamDeckSocket(port,uuid,registerEvent,info) {
 socket = new WebSocket('ws://127.0.0.1:'+port);
 socket.onopen=()=>send({event:registerEvent,uuid});
 socket.onmessage=event=>{try{handle(JSON.parse(event.data));}catch(error){}};
}
window.connectElgatoStreamDeckSocket=connectElgatoStreamDeckSocket;
window.connectSocket=connectElgatoStreamDeckSocket;

