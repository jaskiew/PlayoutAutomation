BEGIN TRANSACTION;
CREATE TABLE archive (
  idArchive                 INTEGER PRIMARY KEY,
  Folder                    TEXT
);

CREATE TABLE archivemedia (
  idArchiveMedia INTEGER PRIMARY KEY,
  MediaGuid BLOB NOT NULL,
  idArchive TEXT,
  MediaName TEXT,
  Folder TEXT,
  FileName TEXT,
  FileSize INTEGER NOT NULL,
  LastUpdated BIGINT NOT NULL,
  Duration BIGINT NOT NULL,
  DurationPlay BIGINT NOT NULL,
  typVideo INTEGER NOT NULL,
  typAudio INTEGER NOT NULL,
  typMedia INTEGER NOT NULL,
  AudioVolume NUMERIC NOT NULL,
  AudioLevelIntegrated NUMERIC NOT NULL,
  AudioLevelPeak NUMERIC NOT NULL,
  statusMedia INTEGER NOT NULL,
  TCStart BIGINT NOT NULL,
  TCPlay BIGINT NOT NULL,
  idProgramme BIGINT NOT NULL,
  idAux TEXT,
  KillDate BIGINT NOT NULL,
  flags INTEGER NOT NULL
);

CREATE INDEX archivemedia_idArchive ON archivemedia (idArchive);
CREATE INDEX archivemedia_MediaGuid ON archivemedia (MediaGuid);

CREATE TABLE asrunlog (
  idAsRunLog INTEGER PRIMARY KEY,
  idEngine BIGINT NOT NULL,
  ExecuteTime BIGINT NOT NULL,
  MediaName TEXT,
  StartTC BIGINT NOT NULL,
  Duration BIGINT NOT NULL,
  idProgramme BIGINT NOT NULL,
  idAuxMedia TEXT,
  idAuxRundown TEXT,
  SecEvents TEXT,
  typVideo INTEGER NOT NULL,
  typAudio INTEGER NOT NULL,
  Flags INTEGER NOT NULL
);

CREATE INDEX asrunlog_ExecuteTime ON asrunlog (ExecuteTime);
CREATE INDEX asrunlog_idEngine ON asrunlog (idEngine);

CREATE TABLE customcommand (
  idCustomCommand INTEGER PRIMARY KEY,
  idEngine BIGINT NOT NULL,
  CommandName TEXT,
  CommandIn TEXT,
  CommandOut TEXT 
);

CREATE TABLE engine (
  idEngine INTEGER PRIMARY KEY,
  Instance BIGINT NOT NULL,
  idServerPRI BIGINT NOT NULL,
  ServerChannelPRI INTEGER NOT NULL,
  idServerSEC BIGINT NOT NULL,
  ServerChannelSEC INTEGER NOT NULL,
  idServerPRV INTEGER NOT NULL,
  ServerChannelPRV INTEGER NOT NULL,
  idArchive BIGINT NOT NULL,
  Config TEXT NOT NULL
);

CREATE TABLE mediasegments (
  idMediaSegment INTEGER PRIMARY KEY,
  MediaGuid BLOB NOT NULL,
  TCIn BIGINT NOT NULL,
  TCOut BIGINT NOT NULL,
  SegmentName TEXT NOT NULL
);

CREATE INDEX mediasegments_MediaGuid ON mediasegments (MediaGuid);

CREATE TABLE media_templated (
  MediaGuid BLOB PRIMARY KEY NOT NULL,
  Method INTEGER NOT NULL,
  TemplateLayer INTEGER NOT NULL,
  ScheduledDelay BIGINT NOT NULL,
  StartType INTEGER NOT NULL,
  Fields TEXT NOT NULL
);

CREATE TABLE rundownevent (
  idRundownEvent INTEGER PRIMARY KEY,
  idEngine BIGINT NOT NULL,
  idEventBinding BIGINT NOT NULL,
  MediaGuid BLOB NOT NULL,
  typEvent INTEGER NOT NULL,
  typStart INTEGER NOT NULL,
  ScheduledTime BIGINT NOT NULL,
  ScheduledDelay BIGINT NOT NULL,
  ScheduledTC BIGINT NOT NULL,
  Duration BIGINT NOT NULL,
  EventName TEXT NOT NULL,
  Layer INTEGER NOT NULL,
  AudioVolume NUMERIC,
  StartTime BIGINT NOT NULL,
  StartTC BIGINT NOT NULL,
  RequestedStartTime BIGINT NOT NULL,
  PlayState INTEGER NOT NULL,
  TransitionTime BIGINT NOT NULL,
  TransitionPauseTime BIGINT NOT NULL,
  typTransition INTEGER NOT NULL,
  idProgramme BIGINT NOT NULL,
  idCustomCommand BIGINT NOT NULL,
  flagsEvent INTEGER,
  idAux TEXT NOT NULL,
  Commands TEXT,
  RouterPort INTEGER NOT NULL
  --KEY idEventBinding (idEventBinding) USING BTREE,
  --KEY id_ScheduledTime (ScheduledTime) USING BTREE,
  --KEY idPlaystate (PlayState) USING BTREE
);

CREATE INDEX rundownevent_idEventBinding ON rundownevent (idEventBinding);
CREATE INDEX rundownevent_ScheduledTime ON rundownevent (ScheduledTime);
CREATE INDEX rundownevent_PlayState ON rundownevent (PlayState);

CREATE TABLE rundownevent_templated (
  idrundownevent_templated BIGINT PRIMARY KEY,
  Method TINYINT NOT NULL,
  TemplateLayer INTEGER NOT NULL,
  Fields TEXT
);

CREATE TABLE server (
  idServer BIGINT PRIMARY KEY,
  typServer INTEGER NOT NULL,
  Config TEXT
);

CREATE TABLE servermedia (
  idserverMedia BIGINT PRIMARY KEY,
  MediaGuid BLOB NOT NULL,
  idServer BIGINT NOT NULL,
  MediaName TEXT,
  Folder TEXT,
  FileName TEXT,
  FileSize BIGINT NOT NULL,
  LastUpdated BIGINT NOT NULL,
  Duration BIGINT NOT NULL,
  DurationPlay BIGINT NOT NULL,
  typVideo INTEGER NOT NULL,
  typAudio INTEGER NOT NULL,
  typMedia INTEGER NOT NULL,
  AudioVolume NUMERIC NOT NULL,
  AudioLevelIntegrated NUMERIC NOT NULL,
  AudioLevelPeak NUMERIC NOT NULL,
  statusMedia INTEGER NOT NULL,
  TCStart BIGINT NOT NULL,
  TCPlay BIGINT NOT NULL,
  idProgramme BIGINT NOT NULL,
  idAux TEXT,
  KillDate BIGINT NOT NULL,
  flags INTEGER NOT NULL
);

CREATE INDEX servermedia_idServer ON servermedia (idServer);
CREATE INDEX servermedia_MediaGuid ON servermedia (MediaGuid);

CREATE TABLE params (
  Section TEXT,
  Key TEXT,
  Value TEXT
  --PRIMARY KEY (Section, Key)
  );

CREATE TABLE aco (
  idACO INTEGER PRIMARY KEY,
  typACO INTEGER NOT NULL,
  Config TEXT
);

CREATE TABLE rundownevent_acl (
  idRundownevent_ACL BIGINT PRIMARY KEY,
  idRundownEvent BIGINT NOT NULL,
  idACO BIGINT NOT NULL,
  ACL BIGINT NOT NULL,
  CONSTRAINT rundownevent_acl_ACO FOREIGN KEY (idACO) REFERENCES aco (idACO) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT rundownevent_acl_RundownEvent FOREIGN KEY (idRundownEvent) REFERENCES rundownevent (idRundownEvent) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE INDEX rundownevent_acl_idRundownEvent ON rundownevent_acl (idRundownEvent);
CREATE INDEX rundownevent_acl_idACO ON rundownevent_acl (idACO);

CREATE TABLE engine_acl (
  idEngine_ACL BIGINT PRIMARY KEY,
  idEngine BIGINT NOT NULL,
  idACO BIGINT NOT NULL,
  ACL BIGINT NOT NULL,
  CONSTRAINT engine_acl_ACO FOREIGN KEY (idACO) REFERENCES aco (idACO) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT engine_acl_Engine FOREIGN KEY (idEngine) REFERENCES engine (idEngine) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE INDEX engine_acl_idEngine ON engine_acl (idEngine);
CREATE INDEX engine_acl_idACO ON engine_acl (idACO);


INSERT INTO params (Section, Key, Value) VALUES ('DATABASE', 'VERSION', '1');
COMMIT