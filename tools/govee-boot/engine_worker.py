"""Direct installed Lenovo SDK client; no Zone UI, tray, proxy or HID writes."""
import ctypes as C,json,os
from pathlib import Path
class Engine:
 def __init__(self):
  base=Path(r'C:\Program Files (x86)\Lenovo\LegionZone\2.0.30.11033\SESDK')
  self.path_handle=os.add_dll_directory(str(base));self.dll=C.WinDLL(str(base/'SEAgent64.dll'))
  self.dll.se_syn_register.argtypes=[C.c_int,C.c_void_p,C.c_void_p];self.dll.se_syn_register.restype=C.c_int
  self.dll.se_syn_send.argtypes=[C.c_int,C.c_char_p,C.c_char_p,C.c_void_p,C.c_int];self.dll.se_syn_send.restype=C.c_int
  self.dll.se_msg_close.argtypes=[];self.dll.se_msg_close.restype=None
  self.client_id=self.dll.se_syn_register(30,None,None)
  if self.client_id<0:raise RuntimeError('Official engine registration failed: '+str(self.client_id))
 def call(self,action,payload=None):
  if action not in {'Get-LightingProfileIndex','Get-LightingProfileDetails','Get-BrightnessLevel','Get-YLogoStatus','Get-GetDynamicLighting','Set-LightingProfileDetails','Set-LightingProfileIndex'}:raise ValueError(action)
  buf=C.create_string_buffer(1048576)
  rc=self.dll.se_syn_send(2015,action.encode(),json.dumps(payload or {}).encode(),buf,len(buf))
  if rc<0 or not buf.value:raise RuntimeError('Official engine request failed: '+str(rc))
  result=json.loads(buf.value)
  if result.get('error_code')!=0 or result.get('items',{}).get('Didsuccess') is not True:raise RuntimeError(str(result))
  return result['items']
 def close(self):self.dll.se_msg_close();self.path_handle.close()
 def __enter__(self):return self
 def __exit__(self,*args):self.close()
 def snapshot(self):
  pid=self.call('Get-LightingProfileIndex')['profileId']
  detail=self.call('Get-LightingProfileDetails',{'F2015':{'profileId':pid}})
  return {'index':pid,'profile':{k:detail[k] for k in ['profileId','layers']},'brightness':self.call('Get-BrightnessLevel'),'logo':self.call('Get-YLogoStatus'),'dynamic':self.call('Get-GetDynamicLighting')}
 def restore(self,before):
  self.call('Set-LightingProfileDetails',{'F2015':before['profile']})
  self.call('Set-LightingProfileIndex',{'F2015':{'profileId':before['index']}})

if __name__=='__main__':
 import sys
 with Engine() as engine:
  print(json.dumps({'ready':True}),flush=True)
  for line in sys.stdin:
   try:
    if len(line)>1000000:raise ValueError('request too large')
    request=json.loads(line)
    result=engine.call(request['action'],request.get('payload'))
    print(json.dumps({'ok':True,'items':result}),flush=True)
   except Exception as e:print(json.dumps({'ok':False,'error':str(e)}),flush=True)
