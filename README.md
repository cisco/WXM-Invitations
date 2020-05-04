# Cisco Webex Experience Management - Invitations Delivery

## Introducing Experience Management invitations solution

In an economy where customer experience trumps price and product, the Cisco Webex Experience Management (referred to as Experience Management here on in the document) platform helps organizations exceed customer expectations and deliver business outcomes through its three pillars of customer experience:

 - Measuring the customer journey: Understanding the customer experience through a continuous collection of moments across their journey.

 - Obtaining a 360-degree view of the customer: Bringing disparate data together to enable actionable insights, proactive support and advanced marketing automation.

 - Becoming predictive: Understanding predictively how changes to the customer experience will affect outcomes and impact financial metrics.
One of the key steps in measuring customer experience is to reach out to customers over various channels such as email, SMS, web intercept etc to solicit feedback. Amongst all the survey distribution channels, email and SMS are 2 of the popular ones.

Global delivery invitation management enables a personalized experience for receiving survey invitations across various channels on the journey using workflows that can be configured and reused across chosen transmission channels like SMS and Email while requiring no PII information in the Experience Management platform.

## Key benefits and features

![](https://cloudcherry.com/docs/delivery-Policy-screen-shot/invitations-delivery-architecture/invitation-delivery-architecture-step1.jpg)

## High level module workflow

The following diagram provides a high-level view of the workflow of the Invitations module.

![](https://cloudcherry.com/docs/delivery-Policy-screen-shot/invitations-delivery-architecture/invitation-delivery-architecture-step2.png)

The Invitations solution would be a single-tenant public or private cloud hosted Invitations module that would interface with the multi-tenant SaaS Experience Management. Everything on the left-hand side of the above diagram encapsulates the Invitations module solution and everything on the right-hand side depicts the multi-tenant SaaS modules.

The various components which are a part of the Invitations module is as given below:
 - Dispatch request: This is the entry point into the Invitations module. A 3rd party system can make an API request to the invitations module to initiate an email/SMS send
 - Sampling: Customers may choose to send only a subset of all the records included in the dispatch request. This can be achieved by setting sampling rules either in Experience Management or can be extended using any custom logic
 - Cross channel token creation: This component interfaces with Experience Management Delivery policy module to create Experience Management unique survey links that will be sent to end customers
 - Database: This component holds all the data needed to be stored for the Invitations module to work.
 - Dispatcher: This component interfaces with 3rd party email and SMS vendors to send emails and SMS
    1. Custom SMTP: Custom SMTP can be set up by providing the details of an email vendor or an email softwares SMTP details (for ex: Outlook/gmail smtp settings or Sengrid SMTP settings). Using Custom SMTP, the dispatcher component processes and sends one email at a time.
    2. Custom SMS: Custom SMS can be set up by configuring the SMS webhook provided by 3rd party SMS vendors or by integrating with 3rd party SMS vendor APIs (for ex: Messagebird, Webtext, Nexmo etc). Using Custom SMS, the dispatcher component processes and sends one SMS at a time.
    3. Bulk Email: Bulk email can be set up by configuring a 3rd party email vendors details. This is an API integration done with Email vendors such as Sparkpost, and others who provides APIs that accepts multiple email requests in one API call. This is particularly used while you are setting up the Invitations module to work at a very large-scale sending millions of emails a day.

Please refer the following documents for consuming this open sourced code base and completing the deployment of the single instance private cloud module.
 - Webex Experience Management invitations Module Architecture - This document details the architecture of the Invitations module in depth. Link -  https://cloudcherry.com/docs/cxsetup/guides/partnerarchitecture/
 - Partner hosted infra provisioning doc - This document explains how to provision the infrastructure required to deploy the Partner hosted components of the Invitations module. Link -  https://cloudcherry.com/docs/cxsetup/guides/partnerinfra/
 - Partner hosted Module deployment guide - This document explains how to deploy the partner hosted components of the invitations module once the infra is provisioned. Link -  https://cloudcherry.com/docs/cxsetup/guides/partnerdeployment/



