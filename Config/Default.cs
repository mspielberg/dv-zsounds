using System.Collections.Generic;
using System.Linq;

namespace DvMod.ZSounds.Config
{
    public static class Default
    {
        public static readonly List<string> DefaultConfigFileContents = new List<string>()
        {
            // v1.0.0
            @"{
    ""version"": 1,

    ""rules"": {
        ""root"": {
            ""type"": ""AllOf"",
            ""rules"": [
                { ""type"": ""If"", ""property"": ""CarType"", ""value"": ""LocoDiesel"",
                    ""rule"": { ""type"": ""Ref"", ""name"": ""loco_de7"" } },
                { ""type"": ""If"", ""property"": ""CarType"", ""value"": ""LocoShunter"",
                    ""rule"": { ""type"": ""Ref"", ""name"": ""loco_de2"" } },
                { ""type"": ""If"", ""property"": ""CarType"", ""value"": ""LocoSteamHeavy"",
                    ""rule"": { ""type"": ""Ref"", ""name"": ""loco_sh282"" } },
            ]
        },

        // locomotives
        ""loco_de2"": {
            ""type"": ""AllOf"",
            ""rules"": [ ""random_diesel_horn"", ""random_de2_engine"" ],
        },
        ""loco_de6"": {
            ""type"": ""AllOf"",
            ""rules"": [ ""random_diesel_horn"", ""random_de6_engine"" ],
            ""sounds"": [ ""EBell"" ],
        },
        ""loco_sh282"": {
            ""type"": ""AllOf"",
            ""rules"": [ ""random_sh282_whistle"" ],
        },

        // randomized sounds
        ""random_diesel_horn"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""AllOf"", ""sounds"": [ ""A200_hit"", ""A200_loop"" ] },
                { ""type"": ""AllOf"", ""sounds"": [ ""RS3L_hit"", ""RS3L_loop"" ] },
            ]
        },
        ""random_de2_engine"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""AllOf"" }, // default engine sounds
                { ""type"": ""AllOf"", ""sounds"": [ ""OM402LA_startup"", ""OM402LA_loop"", ]},
            ],
            ""weights"": [ 1, 3 ]
        },
        ""random_de6_engine"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""AllOf"" }, // default engine sounds
                { ""type"": ""AllOf"", ""sounds"": [ ""EMD645_startup"", ""EMD645_loop"", ""EMD645_shutdown"" ]},
            ]
        },
        ""random_sh282_whistle"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""Sound"", ""name"": ""Hancock_3Chime"" },
                { ""type"": ""Sound"", ""name"": ""Manns_Creek_3Chime"" },
                { ""type"": ""Sound"", ""name"": ""Southern_3Chime"" },
            ]
        }
    },

    ""sounds"": {
        ""EBell"": { ""type"": ""Bell"", ""filename"": ""Sounds\\Graham_White_E_Bell.ogg"" },

        ""A200_hit"": { ""type"": ""HornHit"", ""filename"": ""Sounds\\Leslie_A200_hit.ogg"" },
        ""A200_loop"": { ""type"": ""HornLoop"", ""filename"": ""Sounds\\Leslie_A200_loop.ogg"", ""minPitch"": 0.97, ""maxPitch"": 1 },
        ""RS3L_hit"": { ""type"": ""HornHit"", ""filename"": ""Sounds\\RS3L_start.ogg"" },
        ""RS3L_loop"": { ""type"": ""HornLoop"", ""filename"": ""Sounds\\RS3L_loop.ogg"", ""minPitch"": 0.97, ""maxPitch"": 1 },

        ""EMD645_startup"": { ""type"": ""EngineStartup"", ""filename"": ""Sounds\\645E3_Startup.ogg"", ""fadeStart"": 10, ""fadeDuration"": 2 },
        ""EMD645_loop"": { ""type"": ""EngineLoop"", ""filename"": ""Sounds\\645E3_idle.ogg"" },
        ""EMD645_shutdown"": { ""type"": ""EngineShutdown"", ""filename"": ""Sounds\\645E3_Shutdown.ogg"", ""fadeStart"": 0.27, ""fadeDuration"": 1 },

        ""OM402LA_startup"": { ""type"": ""EngineStartup"", ""filename"": ""Sounds\\OM402LA_Startup.ogg"", ""fadeStart"": 0.18, ""fadeDuration"": 2 },
        ""OM402LA_loop"": { ""type"": ""EngineLoop"", ""filename"": ""Sounds\\OM402LA_Loop.ogg"" },

        ""Hancock_3Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\Hancock_3-Chime.ogg"" },
        ""Manns_Creek_3Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\Manns_Creek_3_Chime.ogg"" },
        ""Southern_3Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\Southern_3_Chime.ogg"" },
    }
}
            ",

            // v1.0.1
            @"{
    ""version"": 1,

    ""rules"": {
        ""root"": {
            ""type"": ""AllOf"",
            ""rules"": [
                { ""type"": ""If"", ""property"": ""CarType"", ""value"": ""LocoDiesel"",
                    ""rule"": { ""type"": ""Ref"", ""name"": ""loco_de6"" } },
                { ""type"": ""If"", ""property"": ""CarType"", ""value"": ""LocoShunter"",
                    ""rule"": { ""type"": ""Ref"", ""name"": ""loco_de2"" } },
                { ""type"": ""If"", ""property"": ""CarType"", ""value"": ""LocoSteamHeavy"",
                    ""rule"": { ""type"": ""Ref"", ""name"": ""loco_sh282"" } }
            ]
        },

        ""loco_de2"": {
            ""type"": ""AllOf"",
            ""rules"": [ ""random_diesel_horn"", ""random_de2_engine"" ]
        },
        ""loco_de6"": {
            ""type"": ""AllOf"",
            ""rules"": [ ""random_de6_bell"", ""random_de6_engine"", ""random_diesel_horn"" ]
        },
        ""loco_sh282"": {
            ""type"": ""AllOf"",
            ""rules"": [ ""random_sh282_whistle"" ]
        },

        ""random_diesel_horn"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""AllOf"", ""sounds"": [ ""A200_hit"", ""A200_loop"" ] },
                { ""type"": ""AllOf"", ""sounds"": [ ""RS3L_hit"", ""RS3L_loop"" ] }
            ]
        },
        ""random_de2_engine"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""AllOf"" },
                { ""type"": ""AllOf"", ""sounds"": [ ""OM402LA_startup"", ""OM402LA_loop"" ]}
            ],
            ""weights"": [ 1, 3 ]
        },
        ""random_de6_bell"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""Sound"", ""name"": ""EBell"" }
            ]
        },
        ""random_de6_engine"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""AllOf"" },
                { ""type"": ""AllOf"", ""sounds"": [ ""EMD645_startup"", ""EMD645_loop"", ""EMD645_shutdown"" ]}
            ]
        },
        ""random_sh282_whistle"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""Sound"", ""name"": ""Hancock_3Chime"" },
                { ""type"": ""Sound"", ""name"": ""Manns_Creek_3Chime"" },
                { ""type"": ""Sound"", ""name"": ""Southern_3Chime"" }
            ]
        }
    },

    ""sounds"": {
        ""EBell"": { ""type"": ""Bell"", ""filename"": ""Sounds\\Graham_White_E_Bell.ogg"" },

        ""A200_hit"": { ""type"": ""HornHit"", ""filename"": ""Sounds\\Leslie_A200_hit.ogg"" },
        ""A200_loop"": { ""type"": ""HornLoop"", ""filename"": ""Sounds\\Leslie_A200_loop.ogg"", ""minPitch"": 0.97, ""maxPitch"": 1 },
        ""RS3L_hit"": { ""type"": ""HornHit"", ""filename"": ""Sounds\\RS3L_start.ogg"" },
        ""RS3L_loop"": { ""type"": ""HornLoop"", ""filename"": ""Sounds\\RS3L_loop.ogg"", ""minPitch"": 0.97, ""maxPitch"": 1 },

        ""EMD645_startup"": { ""type"": ""EngineStartup"", ""filename"": ""Sounds\\645E3_Startup.ogg"", ""fadeStart"": 10, ""fadeDuration"": 2 },
        ""EMD645_loop"": { ""type"": ""EngineLoop"", ""filename"": ""Sounds\\645E3_idle.ogg"" },
        ""EMD645_shutdown"": { ""type"": ""EngineShutdown"", ""filename"": ""Sounds\\645E3_Shutdown.ogg"", ""fadeStart"": 0.27, ""fadeDuration"": 1 },

        ""OM402LA_startup"": { ""type"": ""EngineStartup"", ""filename"": ""Sounds\\OM402LA_Startup.ogg"", ""fadeStart"": 0.18, ""fadeDuration"": 2 },
        ""OM402LA_loop"": { ""type"": ""EngineLoop"", ""filename"": ""Sounds\\OM402LA_Loop.ogg"" },

        ""Hancock_3Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\Hancock_3-Chime.ogg"" },
        ""Manns_Creek_3Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\Manns_Creek_3_Chime.ogg"" },
        ""Southern_3Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\Southern_3_Chime.ogg"" }
    }
}
            ",

            // v1.1.0
            @"{
    ""version"": 1,

    ""rules"": {
        ""root"": {
            ""type"": ""AllOf"",
            ""rules"": [
                { ""type"": ""If"", ""property"": ""CarType"", ""value"": ""LocoDiesel"",
                    ""rule"": { ""type"": ""Ref"", ""name"": ""loco_de6"" } },
                { ""type"": ""If"", ""property"": ""CarType"", ""value"": ""LocoShunter"",
                    ""rule"": { ""type"": ""Ref"", ""name"": ""loco_de2"" } },
                { ""type"": ""If"", ""property"": ""CarType"", ""value"": ""LocoSteamHeavy"",
                    ""rule"": { ""type"": ""Ref"", ""name"": ""loco_sh282"" } }
            ]
        },

        ""loco_de2"": {
            ""type"": ""AllOf"",
            ""rules"": [ ""random_de6_bell"", ""random_diesel_horn"", ""random_de2_engine"" ]
        },
        ""loco_de6"": {
            ""type"": ""AllOf"",
            ""rules"": [ ""random_de6_bell"", ""random_de6_engine"", ""random_diesel_horn"" ]
        },
        ""loco_sh282"": {
            ""type"": ""AllOf"",
            ""rules"": [ ""random_sh282_whistle"" ]
        },

        ""random_diesel_horn"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""AllOf"", ""sounds"": [ ""A200_hit"", ""A200_loop"" ] },
                { ""type"": ""AllOf"", ""sounds"": [ ""RS3L_hit"", ""RS3L_loop"" ] }
            ]
        },
        ""random_de2_engine"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""AllOf"" },
                { ""type"": ""AllOf"", ""sounds"": [ ""OM402LA_startup"", ""OM402LA_loop"" ]}
            ],
            ""weights"": [ 1, 3 ]
        },
        ""random_de6_bell"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""Sound"", ""name"": ""EBell"" }
            ]
        },
        ""random_de6_engine"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""AllOf"" },
                { ""type"": ""AllOf"", ""sounds"": [ ""EMD645_startup"", ""EMD645_loop"", ""EMD645_shutdown"" ]}
            ]
        },
        ""random_sh282_whistle"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""Sound"", ""name"": ""Hancock_3Chime"" },
                { ""type"": ""Sound"", ""name"": ""Manns_Creek_3Chime"" },
                { ""type"": ""Sound"", ""name"": ""Southern_3Chime"" }
            ]
        }
    },

    ""sounds"": {
        ""EBell"": { ""type"": ""Bell"", ""filename"": ""Sounds\\Graham_White_E_Bell.ogg"" },

        ""A200_hit"": { ""type"": ""HornHit"", ""filename"": ""Sounds\\Leslie_A200_hit.ogg"" },
        ""A200_loop"": { ""type"": ""HornLoop"", ""filename"": ""Sounds\\Leslie_A200_loop.ogg"", ""minPitch"": 0.97, ""maxPitch"": 1 },
        ""RS3L_hit"": { ""type"": ""HornHit"", ""filename"": ""Sounds\\RS3L_start.ogg"" },
        ""RS3L_loop"": { ""type"": ""HornLoop"", ""filename"": ""Sounds\\RS3L_loop.ogg"", ""minPitch"": 0.97, ""maxPitch"": 1 },

        ""EMD645_startup"": { ""type"": ""EngineStartup"", ""filename"": ""Sounds\\645E3_Startup.ogg"", ""fadeStart"": 10, ""fadeDuration"": 2 },
        ""EMD645_loop"": { ""type"": ""EngineLoop"", ""filename"": ""Sounds\\645E3_idle.ogg"", ""minPitch"": 1, ""maxPitch"": 3 },
        ""EMD645_shutdown"": { ""type"": ""EngineShutdown"", ""filename"": ""Sounds\\645E3_Shutdown.ogg"", ""fadeStart"": 0.27, ""fadeDuration"": 1 },

        ""OM402LA_startup"": { ""type"": ""EngineStartup"", ""filename"": ""Sounds\\OM402LA_Startup.ogg"", ""fadeStart"": 0.18, ""fadeDuration"": 2 },
        ""OM402LA_loop"": { ""type"": ""EngineLoop"", ""filename"": ""Sounds\\OM402LA_Loop.ogg"", ""minPitch"": 1, ""maxPitch"": 1.7, ""minVolume"": 0.1, ""maxVolume"": 0.4 },

        ""Hancock_3Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\Hancock_3-Chime.ogg"" },
        ""Manns_Creek_3Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\Manns_Creek_3_Chime.ogg"" },
        ""Southern_3Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\Southern_3_Chime.ogg"" }
    }
}
            ",

            // v1.4.2
            @"{
    ""version"": 1,

    ""rules"": {
        ""root"": {
            ""type"": ""AllOf"",
            ""rules"": [
                { ""type"": ""If"", ""property"": ""CarType"", ""value"": ""LocoDiesel"",
                    ""rule"": { ""type"": ""Ref"", ""name"": ""loco_de6"" } },
                { ""type"": ""If"", ""property"": ""CarType"", ""value"": ""LocoShunter"",
                    ""rule"": { ""type"": ""Ref"", ""name"": ""loco_de2"" } },
                { ""type"": ""If"", ""property"": ""CarType"", ""value"": ""LocoSteamHeavy"",
                    ""rule"": { ""type"": ""Ref"", ""name"": ""loco_sh282"" } }
            ]
        },

        ""loco_de2"": {
            ""type"": ""AllOf"",
            ""rules"": [ ""random_de6_bell"", ""random_diesel_horn"", ""random_de2_engine"" ]
        },
        ""loco_de6"": {
            ""type"": ""AllOf"",
            ""rules"": [ ""random_de6_bell"", ""random_de6_engine"", ""random_diesel_horn"" ]
        },
        ""loco_sh282"": {
            ""type"": ""AllOf"",
            ""rules"": [ ""random_sh282_whistle"" ]
        },

        ""random_diesel_horn"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""AllOf"", ""sounds"": [ ""A200_hit"", ""A200_loop"" ] },
                { ""type"": ""AllOf"", ""sounds"": [ ""RS3L_hit"", ""RS3L_loop"" ] }
            ]
        },
        ""random_de2_engine"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""AllOf"" },
                { ""type"": ""AllOf"", ""sounds"": [ ""OM402LA_startup"", ""OM402LA_loop"" ]}
            ],
            ""weights"": [ 1, 3 ]
        },
        ""random_de6_bell"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""Sound"", ""name"": ""EBell"" }
            ]
        },
        ""random_de6_engine"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""AllOf"" },
                { ""type"": ""AllOf"", ""sounds"": [ ""EMD645_startup"", ""EMD645_loop"", ""EMD645_shutdown"" ]}
            ]
        },
        ""random_sh282_whistle"": {
            ""type"": ""OneOf"",
            ""rules"": [
                { ""type"": ""Sound"", ""name"": ""Hancock_3Chime"" },
                { ""type"": ""Sound"", ""name"": ""Manns_Creek_3Chime"" },
                { ""type"": ""Sound"", ""name"": ""Southern_3Chime"" }
            ]
        }
    },

    ""sounds"": {
        ""EBell"": { ""type"": ""Bell"", ""filename"": ""Sounds\\Graham_White_E_Bell.ogg"" },

        ""A200_hit"": { ""type"": ""HornHit"", ""filename"": ""Sounds\\Leslie_A200_hit.ogg"" },
        ""A200_loop"": { ""type"": ""HornLoop"", ""filename"": ""Sounds\\Leslie_A200_loop.ogg"", ""minPitch"": 0.97, ""maxPitch"": 1 },
        ""RS3L_hit"": { ""type"": ""HornHit"", ""filename"": ""Sounds\\RS3L_start.ogg"" },
        ""RS3L_loop"": { ""type"": ""HornLoop"", ""filename"": ""Sounds\\RS3L_loop.ogg"", ""minPitch"": 0.97, ""maxPitch"": 1 },

        ""EMD645_startup"": { ""type"": ""EngineStartup"", ""filename"": ""Sounds\\645E3_Startup.ogg"", ""fadeStart"": 10, ""fadeDuration"": 2 },
        ""EMD645_loop"": { ""type"": ""EngineLoop"", ""filename"": ""Sounds\\645E3_idle.ogg"", ""minPitch"": 1, ""maxPitch"": 3 },
        ""EMD645_shutdown"": { ""type"": ""EngineShutdown"", ""filename"": ""Sounds\\645E3_Shutdown.ogg"", ""fadeStart"": 0.27, ""fadeDuration"": 1 },

        ""OM402LA_startup"": { ""type"": ""EngineStartup"", ""filename"": ""Sounds\\OM402LA_Startup.ogg"", ""fadeStart"": 0.18, ""fadeDuration"": 2 },
        ""OM402LA_loop"": { ""type"": ""EngineLoop"", ""filename"": ""Sounds\\OM402LA_Loop.ogg"", ""minPitch"": 1, ""maxPitch"": 1.7, ""minVolume"": 0.1, ""maxVolume"": 0.4 },

        ""Hancock_3Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\Hancock_3-Chime.ogg"" },
        ""Manns_Creek_3Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\Manns_Creek_3_Chime.ogg"" },
        ""Southern_3Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\Southern_3_Chime.ogg"" },
        ""SF_6Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\SF_6_chime.ogg"" },
        ""DRGW_487_5Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\DRGW_487_5_chime.ogg"" },
        ""NW_Hooter"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\NW_Hooter.ogg"" },
        ""Powell_3Chime"": { ""type"": ""Whistle"", ""filename"": ""Sounds\\powell_3.ogg"" },
    }
}
            ",
        };

        public static string CurrentDefaultConfigFile = DefaultConfigFileContents.Last();

        public static bool IsDefaultConfigFile(string path)
        {
            var content = System.IO.File.ReadAllText(path);
            return DefaultConfigFileContents.Contains(content);
        }
    }
}