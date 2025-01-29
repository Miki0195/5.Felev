## Alap funkcionalitások

| USER | USER STORY | PRIORITY | NOTES |
| :--: | :--------: | :------: | :---: |
| admin | As an admin, after purchase I want to receive a custom product key, so I can register my company with that key and use the system. | MUST HAVE | Each key is unique and is paired to the company in the database. |
| admin | As an admin, I want to be able to create user profiles, give roles for the employees. | MUST HAVE | Each employee profile is generated this way, regardless of it's authorization level. |
| admin | As an admin, I want to deactivate or delete user profiles, so that inactive employees no longer have access to the system. | MUST HAVE | Deactivated users should not lose historical data but will lose access. |
| admin | As an admin, I want to reset user passwords when necessary, so that employees can regain access if they face issues. | MUST HAVE | Password reset triggers a temporary password sent via email to the user. |
| user | As a user, I want to be able to login to the system with the credentials given by the admin. | MUST HAVE | After the admin creates the user profiles an email is automatically sent with the credentials to the user. The password given here is temporary, the user is redirected to give his own password. |
| user | As a user, if I forget my passsword I have an option to contact the admin to get a new password. | MUST HAVE | I will get an email with a temporary password, which then I can change. |
| user | As a user, I want to be automatically logged out after a period of inactivity, so that my account remains secure. | MUST HAVE | The inactivity timeout duration should be configurable by the admin. |
| user | As a user, I want to update my profile information (e.g., name, contact info), so that my personal details remain accurate. | MUST HAVE | Profile updates should trigger a notification to the admin for tracking changes. |
| user | As a user, I want to see a dashboard after login that provides an overview of my schedule, tasks, and notifications. | MUST HAVE | The dashboard is role-specific and dynamically adjusts based on the user’s access level. |

## Alkalmazottak kezelése

| USER | USER STORY | PRIORITY | NOTES |
| :--: | :--------: | :------: | :---: |
| employee | As an employee, I want to view my weekly and monthly work schedule online, so that I can plan my personal time around it. | MUST HAVE | Basic functionality for employee management. This should allow employees to see updated schedules in real-time. |
| employer | As an employer, I want to create and publish weekly and monthly schedules for my employees, so that they know when they need to work. | MUST HAVE | The schedule should be quick to update and send notifications to employees when any changes are made. |
| employee | As an employee, I want to request a day off or vacation directly through the system, so that I can easily communicate my availability to my employer. | MUST HAVE | The status of the request (accepted/rejected) must be traceable. The employer should be notified about the request. | 
| employer | As an employer, I want to review and approve or reject day-off or vacation requests, so that I can ensure adequate staffing levels. | MUST HAVE | Accepted vacations are automatically added to the employee's schedule and visible in the calendar. |
| employee | As an employee, I want to report my illness and upload a medical certificate, so that my employer is informed about my unavailability. | MUST HAVE | The system can automatically warn the worker if the certificate is missing and needs to be uploaded, which he receives a reminder of every day. |
| employer | As an employer, I want to receive notifications when an employee reports illness and uploads medical documentation, so that I can adjust the work schedule accordingly. | MUST HAVE | The system should provide the possibility for the employer to manage and archive documents. |
| employee | As an employee, I want to clock in and clock out digitally, so that my working hours are accurately recorded in the system. | MUST HAVE | The employee can refer back to their own time log to verify the recorded data. |
| employer | As an employer, I want to monitor and review employees' clock-in and clock-out records, so that I can track attendance and calculate wages correctly. | MUST HAVE | This data can be exported. |
| employer | As an employer, I want to generate detailed reports about employees' working hours, performance, absences, and sick leaves, so that I can make data-driven decisions. | MUST HAVE | These reports should be customizable for time intervals. |
| employee | As an employee, I want to communicate directly with my employer through the system, so that I can quickly clarify work-related questions. | MUST HAVE | Basic functionality. |
| employer | As an employer, I want to send announcements or individual messages to employees, so that I can inform them about schedule changes or other important updates. | MUST HAVE | The messages can be flaged, so when they are, the employee gets notification about them. |
| employee | As an employee, I want to see only the information and options relevant to my role, so that I am not overwhelmed by unnecessary data. | MUST HAVE | Basic functiionality. |
| employer | As an employer, I want to define teams and assign team leaders with limited permissions, so that they can manage their teams independently. | MUST HAVE | Group leaders can only see their own group's assignments and reports. |
| admin | As a company administrator, I want to have full access to the organization's structure and employee data, so that I can make strategic decisions. | MUST HAVE | Only the highest level of authorization should have full data access. |

## Home Office and handling Tasks
| USER | USER STORY | PRIORITY | NOTES |
| :--: | :--------: | :------: | :---: |
| user |      | 42.99 | asd |
| user |      | 42.99 | asd |