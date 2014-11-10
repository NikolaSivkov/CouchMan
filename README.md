#CouchMan
========

Powershell cmdlet to help you keep track of your CouchBase views


###Sample bucket configuration file
========

You should place 1 file 
The values in this json that are 0, null or false can be omitted 


#####Short Version : 

```json
{
	"Name":"MyBucket", 						// Some bucket name if not set bucket folder name will be used.
	"BucketType":2, 						// Empty = 0, Memcached = 1, Membase = 2,
	"AuthType":2, 							// Empty = 0, None =1,Sasl = 2
	 
	"Password":"SuperSecretPassword",		
	"SaslPassword":"SuperSecretPassword", 
	"Quota":{
				"RAM":0,   					// this valie MUST be set, the value must be a long (int64). It represents megabytes.
			}
}
```

#####Long Version :
```json
{
	"Name":"MyBucket", 					// Some bucket name
	"BucketType":2, 					// Empty = 0, Memcached = 1, Membase = 2,
	"AuthType":2, 						// Empty = 0, None =1,Sasl = 2
	"FlushOption":0,					// Disabled = 0, Enabled = 1,
	"ProxyPort":0,		
	"Password":"SuperSecretPassword",		
	"SaslPassword":"SuperSecretPassword",		
	"ValidationErrors":null,
	"Nodes":null,		
	"BasicStats":null,		
	"ReplicaIndex":false,		
	"Uri":null,		
	"StreamingUri":null,		
	"LocalRandomKeyUri":null,		
	"Controllers":null,		
	"Stats":null,		
	"DDocs":null,		
	"NodeLocator":null,		
	"AutoCompactionSettings":false,
	"FastWarmupSettings":false,
	"ReplicaNumber":0, 						// you need 2 or more nodes for this to work.
	"Quota":{
				"RAM":0,   					// this valie MUST be set, the value must be a long (int64). It represents megabytes.
				"RawRAM":0 					// this value can be omitted
			},
	"BucketCapabilitiesVer":null,
	"BucketCapabilities":null,
	"VBucketServerMap":null
}
```

