BEGIN TRANSACTION;
CREATE TABLE archive (
  idArchive                 INTEGER PRIMARY KEY,
  Folder                    TEXT
);

CREATE TABLE archivemedia (
  idArchiveMedia INTEGER PRIMARY KEY,
  MediaGuid BLOB,
  idArchive TEXT,
  MediaName TEXT,
  Folder TEXT,
  FileName TEXT,
  FileSize INTEGER,
  LastUpdated BIGINT,
  Duration BIGINT,
  DurationPlay BIGINT,
  typVideo INTEGER,
  typAudio INTEGER,
  typMedia INTEGER,
  AudioVolume NUMERIC,
  AudioLevelIntegrated NUMERIC,
  AudioLevelPeak NUMERIC,
  statusMedia INTEGER,
  TCStart BIGINT,
  TCPlay BIGINT,
  idProgramme BIGINT,
  idAux TEXT,
  KillDate BIGINT,
  flags INTEGER
);

CREATE INDEX archivemedia_idArchive ON archivemedia (idArchive);
CREATE INDEX archivemedia_MediaGuid ON archivemedia (MediaGuid);

CREATE TABLE asrunlog (
  idAsRunLog INTEGER PRIMARY KEY,
  idEngine BIGINT,
  ExecuteTime BIGINT,
  MediaName TEXT,
  StartTC BIGINT,
  Duration BIGINT,
  idProgramme BIGINT,
  idAuxMedia TEXT,
  idAuxRundown TEXT,
  SecEvents TEXT,
  typVideo INTEGER,
  typAudio INTEGER,
  Flags INTEGER
);

CREATE INDEX asrunlog_ExecuteTime ON asrunlog (ExecuteTime);
CREATE INDEX asrunlog_idEngine ON asrunlog (idEngine);

CREATE TABLE customcommand (
  idCustomCommand INTEGER PRIMARY KEY,
  idCustomCommand BIGINT,
  idEngine BIGINT,
  CommandName TEXT,
  CommandIn TEXT,
  CommandOut TEXT,
);

CREATE TABLE engine (
  idEngine INTEGER PRIMARY KEY,
  Instance BIGINT,
  idServerPRI BIGINT,
  ServerChannelPRI INTEGER,
  idServerSEC BIGINT,
  ServerChannelSEC INTEGER,
  idServerPRV INTEGER,
  ServerChannelPRV INTEGER,
  idArchive BIGINT,
  Config TEXT
);

CREATE TABLE mediasegments (
  idMediaSegment INTEGER PRIMARY KEY,
  MediaGuid BLOB,
  TCIn BIGINT,
  TCOut BIGINT,
  SegmentName TEXT
);

CREATE INDEX mediasegments_MediaGuid ON mediasegments (MediaGuid);

CREATE TABLE media_templated (
  MediaGuid BLOB PRIMARY KEY,
  Method INTEGER,
  TemplateLayer INTEGER,
  ScheduledDelay BIGINT,
  StartType INTEGER,
  Fields TEXT
);

CREATE TABLE rundownevent (
  idRundownEvent INTEGER PRIMARY KEY,
  idEngine BIGINT,
  idEventBinding BIGINT,
  MediaGuid BLOB,
  typEvent INTEGER,
  typStart INTEGER,
  ScheduledTime BIGINT,
  ScheduledDelay BIGINT,
  ScheduledTC BIGINT,
  Duration BIGINT,
  EventName TEXT,
  Layer INTEGER,
  AudioVolume NUMERIC,
  StartTime BIGINT,
  StartTC BIGINT,
  RequestedStartTime BIGINT,
  PlayState INTEGER,
  TransitionTime BIGINT,
  TransitionPauseTime BIGINT,
  typTransition INTEGER,
  idProgramme BIGINT,
  idCustomCommand BIGINT,
  flagsEvent INTEGER,
  idAux TEXT,
  Commands TEXT,
  RouterPort INTEGER
  --KEY idEventBinding (idEventBinding) USING BTREE,
  --KEY id_ScheduledTime (ScheduledTime) USING BTREE,
  --KEY idPlaystate (PlayState) USING BTREE
);

CREATE INDEX rundownevent_idEventBinding ON rundownevent (idEventBinding);
CREATE INDEX rundownevent_ScheduledTime ON rundownevent (ScheduledTime);
CREATE INDEX rundownevent_PlayState ON rundownevent (PlayState);

CREATE TABLE rundownevent_templated (
  idrundownevent_templated BIGINT PRIMARY KEY,
  Method TINYINT,
  TemplateLayer INTEGER,
  Fields TEXT
);

CREATE TABLE server (
  idServer BIGINT PRIMARY KEY,
  typServer INTEGER,
  Config TEXT
);

CREATE TABLE servermedia (
  idserverMedia BIGINT PRIMARY KEY,
  MediaGuid BLOB,
  idServer BIGINT,
  MediaName TEXT,
  Folder TEXT,
  FileName TEXT,
  FileSize BIGINT,
  LastUpdated BIGINT,
  Duration BIGINT,
  DurationPlay BIGINT,
  typVideo INTEGER,
  typAudio INTEGER,
  typMedia INTEGER,
  AudioVolume NUMERIC,
  AudioLevelIntegrated NUMERIC,
  AudioLevelPeak NUMERIC,
  statusMedia INTEGER,
  TCStart BIGINT,
  TCPlay BIGINT,
  idProgramme BIGINT,
  idAux TEXT,
  KillDate BIGINT,
  flags INTEGER
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
  typACO INTEGER,
  Config TEXT
);

CREATE TABLE rundownevent_acl (
  idRundownevent_ACL BIGINT PRIMARY KEY,
  idRundownEvent BIGINT,
  idACO BIGINT,
  ACL BIGINT,
  CONSTRAINT rundownevent_acl_ACO FOREIGN KEY (idACO) REFERENCES aco (idACO) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT rundownevent_acl_RundownEvent FOREIGN KEY (idRundownEvent) REFERENCES rundownevent (idRundownEvent) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE INDEX rundownevent_acl_idRundownEvent ON rundownevent_acl (idRundownEvent);
CREATE INDEX rundownevent_acl_idACO ON rundownevent_acl (idACO);

CREATE TABLE engine_acl (
  idEngine_ACL BIGINT PRIMARY KEY,
  idEngine BIGINT,
  idACO BIGINT,
  ACL BIGINT,
  CONSTRAINT engine_acl_ACO FOREIGN KEY (idACO) REFERENCES aco (idACO) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT engine_acl_Engine FOREIGN KEY (idEngine) REFERENCES engine (idEngine) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE INDEX engine_acl_idEngine ON engine_acl (idEngine);
CREATE INDEX engine_acl_idACO ON engine_acl (idACO);


INSERT INTO params (Section, Key, Value) VALUES ('DATABASE', 'VERSION', '12');