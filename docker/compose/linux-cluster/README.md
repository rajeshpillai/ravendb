# Simple docker compose setup for 3 node cluster

Note: Please remember to put *license.env* file in this directory. It should contain a row setting `RAVEN_License` environment variable containing license information e.g.
```
RAVEN_License={"Id": "LICENSEID", "Name": "Testing", "Keys": [ ... ]}
```

## Create and run cluster
```
.\run.ps1 [-DontSetupCluster] [-StartBrowser]
```

```
-DontSetupCluster - just create nodes without setting them up
-StartBrowser - starts browser with first node's Studio
```

## Destroy cluster
```
.\destroy.ps1
```