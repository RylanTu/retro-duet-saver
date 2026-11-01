import csv,json
from pathlib import Path
root=Path('output/cache-benchmark')
result={}
for f in root.glob('*.csv'):
 if '-gpu' in f.stem: continue
 rows=list(csv.DictReader(f.open(encoding='utf-8-sig')))
 if not rows: continue
 vals={k:sum(float(r[k]) for r in rows)/len(rows) for k in ['cpu_total_percent','working_mb','private_mb']}
 vals['callback_hz']=sum(float(r['ui_callbacks']) for r in rows)/sum(float(r['elapsed_s']) for r in rows)
 gpu=root/(f.stem+'-gpu.csv')
 if gpu.exists():
  gs=list(csv.DictReader(gpu.open(encoding='utf-8-sig')))
  for name,pattern,scale in [('gpu_3d_percent','engtype_3d',1),('vram_mb','dedicated usage',1048576),('shared_gpu_mb','shared usage',1048576)]:
   a=[float(g['value'])/scale for g in gs if pattern in g['path'].lower() and float(g['seconds'])>5]
   if a: vals[name]=sum(a)/len(a)
 result[f.stem]=vals
print(json.dumps(result,indent=2))
(root/'summary.json').write_text(json.dumps(result,indent=2))
