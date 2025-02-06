## Bacis functionalities

| USER | USER STORY | PRIORITY | NOTES |
| :--: | :--------: | :------: | :---: |
| admin | As an admin, after purchase I want to receive a custom product key, so I can register my company with that key and use the system. | MUST HAVE | Each key is unique and is paired to the company in the database. |
| admin | As an admin, I want to be able to create user profiles, give roles for the employees. | MUST HAVE | Each employee profile is generated this way, regardless of it's authorization level. |
| admin | As an admin, I want to deactivate or delete user profiles, so that inactive employees no longer have access to the system. | MUST HAVE | Deactivated users should not lose historical data but will lose access. |
| admin | As an admin, I want to reset user passwords when necessary, so that employees can regain access if they face issues. | MUST HAVE | Password reset triggers a temporary password sent via email to the user. |
| user | As a user, I want to be able to login to the system with the credentials given by the admin. | MUST HAVE | After the admin creates the user profiles an email is automatically sent with the credentials to the user. The password given here is temporary, the user is redirected to give his own password. |
| user | As a user, I want to be able to login to the system with the credentials given by the admin. | MUST HAVE | After the admin creates the user profiles an email is automatically sent with the credentials to the user. The password given here is temporary, the user is redirected to give his own password. |
| user | As a user, if I forget my passsword I have an option to contact the admin to get a new password. | MUST HAVE | I will get an email with a temporary password, which then I can change. |
| user | As a user, I want to be automatically logged out after a period of inactivity, so that my account remains secure. | MUST HAVE | The inactivity timeout duration should be configurable by the admin. |
| user | As a user, I want to update my profile information (e.g., name, contact info), so that my personal details remain accurate. | MUST HAVE | Profile updates should trigger a notification to the admin for tracking changes. |
| user | As a user, I want to see a dashboard after login that provides an overview of my schedule, tasks, and notifications. | MUST HAVE | The dashboard is role-specific and dynamically adjusts based on the user’s access level. |

## Handling Employees

| USER | USER STORY | PRIORITY | NOTES |
| :--: | :--------: | :------: | :---: |
| employee | As an employee, I want to view my weekly and monthly work schedule online, so that I can plan my personal time around it. | MUST HAVE | Basic functionality for employee management. This should allow employees to see updated schedules in real-time. |
| employer | As an admin, I want to create and publish weekly and monthly schedules for my employees, so that they know when they need to work. | MUST HAVE | The schedule should be quick to update and send notifications to employees when any changes are made. |
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

## Home Office And Handling Tasks
| USER | USER STORY | PRIORITY | NOTES |
| :--: | :--------: | :------: | :---: |
| employer | As an employer, I want to create tasks and assign them to specific employees, so that I can ensure work is distributed efficiently. | MUST HAVE | Tasks should have descriptions, priority levels, and deadlines. |
| employer | As an employer, I want to assign multiple employees to a single task, so that team collaboration is possible. | MUST HAVE | Basic functionality. |
| employee | As an employee, I want to receive notifications when I am assigned a new task, so that I am aware of my responsibilities. | MUST HAVE | Notifications should be sent via email and displayed on the dashboard. |
| employee | As an employee, I want to see all my assigned tasks in a structured list, so that I can prioritize my work efficiently. | MUST HAVE | Tasks should be filterable by priority, deadline, and status. |
| employee | As an employee, I want to mark a task as in progress or completed, so that my employer knows my current workload. | MUST HAVE | The system should update the task status in real-time. |
| employee | As an employee, I want to log the time I spend on each task, so that my work hours are accurately recorded. | MUST HAVE | The system should support automatic tracking. |
| employer | As an employer, I want to see detailed reports on the time employees spend on tasks, so that I can assess efficiency and workload distribution. | MUST HAVE | Reports should be available in both individual and team views. |
| employer | As an employer, I want to set deadlines for tasks, so that employees complete them on time. | MUST HAVE | Deadlines should be visible on the employee’s dashboard. |
| employee | As an employee, I want to receive reminders when a task deadline is approaching, so that I can complete my work on time. | MUST HAVE | Reminders should be sent via email and in-app notifications. |
| employer | As an employer, I want to allow employees to request deadline extensions, so that they can justify the need for more time. | MUST HAVE | The system should notify managers when an extension request is submitted. |
| user | story | MUST HAVE | notes |

## Developer Usage
| USER | USER STORY | PRIORITY | NOTES |
| :--: | :--------: | :------: | :---: |
| developer | As a developer, I want to view a list of all registered companies and employees, so that I can monitor the system’s user base. | MUST HAVE | The developer should not have access to sensitive company or employee data. |
| developer | As a developer, I want to search and filter companies based on their registration date and activity status, so that I can identify inactive companies. | MUST HAVE | The system should provide filters for different criteria (e.g., active, inactive, newly registered). |
| developer | As a developer, I want to manage license keys, so that I can activate or deactivate company access when needed. | MUST HAVE | Each company should have a unique license key stored securely in the database. |

## Video Conferences
| USER | USER STORY | PRIORITY | NOTES |
| :--: | :--------: | :------: | :---: |
| admin | As an admin, I want to schedule meetings within the system, so that my team can have organized discussions. | COULD HAVE | Meeting invitations should be sent automatically to participants. |
| admin | As an admin, I want to send meeting reminders to participants, so that they don’t forget scheduled discussions. | COULD HAVE | Notifications should be sent via email and in-app alerts. |
| user | As a user, I want to join scheduled video meetings directly from the system, so that I don’t need to switch between multiple applications. | COULD HAVE | The system should provide a direct link to the meeting within the dashboard. |

## Extra Functionalities
| USER | USER STORY | PRIORITY | NOTES |
| :--: | :--------: | :------: | :---: |
| user | As a user, I want to receive notifications for task assignments, schedule changes, and meeting invitations, so that I stay informed about important updates. | MUST HAVE | Notifications should be sent via email, in-app alerts, and optionally as mobile push notifications. |
| employer | As an employer, I want to configure which types of notifications are sent to employees, so that they receive only relevant information. | MUST HAVE | The notification settings should allow customization based on priority and category. |
| user | As a user, I want to mark notifications as read or unread, so that I can keep track of important updates. | MUST HAVE | Unread notifications should remain highlighted until reviewed. |
| user | As a user, I want to see my tasks, meetings, and approved leave days in a calendar view, so that I can easily manage my schedule. | MUST HAVE | The calendar should display color-coded events for different categories. |
| employer | As an employer, I want to schedule company-wide events and have them appear in employees’ calendars, so that all team members are aware of key dates. | MUST HAVE | Employees should receive notifications when an event is added to their calendar. |
| user | As a user, I want to export my work schedule to Google Calendar or Outlook, so that I can synchronize it with my personal calendar. | COULD HAVE | The system should generate an .ics file for easy calendar import. |
| employer | As an employer, I want to require employees working remotely to check in at specific intervals, so that I can monitor their work progress. | MUST HAVE | Employees should receive reminders when a check-in is due. |
| user | As a user, I want to confirm my presence by checking in at designated times, so that my employer knows I am actively working. | MUST HAVE | The check-in system should be easy to use and non-intrusive. |
| employer | As an employer, I want to receive alerts if an employee repeatedly misses check-ins, so that I can follow up on their attendance. | MUST HAVE | Missed check-ins should be logged and reported to supervisors. |
| employer | As an employer, I want to assign role-based permissions to employees, so that they only access relevant data and features. | MUST HAVE | Permissions should be adjustable per department or role. |
| employer | As an employer, I want to restrict managers from accessing employee personal data, so that sensitive information remains protected. | MUST HAVE | Only HR and admins should have access to personal records. |
| user | As a user, I want to request additional permissions if needed, so that I can access the tools required for my job. | COULD HAVE | Requests should be reviewed and approved by administrators. |
| employer | As an employer, I want to track employee task completion rates and efficiency, so that I can evaluate performance objectively. | MUST HAVE | Reports should include average task completion time and overdue tasks. |
| employer | As an employer, I want to reward employees who consistently meet deadlines, so that I can encourage high performance. | COULD HAVE | Bonuses or recognition badges should be awarded automatically. |
| user | As a user, I want to view my performance metrics and feedback, so that I can track my progress and identify areas for improvement. | MUST HAVE | Employees should see historical performance trends. |


