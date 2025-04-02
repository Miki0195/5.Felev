select * from aspnetroles;
select * from attendances;
select * from aspnetusers;
delete from attendances where id=47;
select * from notifications;
select * from videoconferences where isended=0;
select * from videoconferences;

delete from notifications;
delete from videoconferences where callid='6ef762a7-fca3-49b8-95c4-3ddb1892f02f';
show tables;
select * from refreshtokens;
delete from refreshtokens where userid="0d012124-6090-4bc7-aa93-9bf4d4480d2b";

select * from VideoCallInvitations;
delete from VideoCallInvitations where id=430;
select * from owneractivitylogs;
delete from aspnetusers where id="33d10f05-4601-49bb-a37f-96a8217615d7";

select * from licensekeys;
select * from DashboardLayouts where id="81b922fd-f726-49a6-a292-7c01b38fe17e";
select * from DashboardLayouts;

select * from tasks where ProjectId=11 order by 1;
select count(*) from tasks where ProjectId=11;

UPDATE tasks
SET TimeSpent = 92
WHERE Id = 17;