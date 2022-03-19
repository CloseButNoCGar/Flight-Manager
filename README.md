# Flight-Manager

This repository contains the code for a Windows Forms flight manager to be used in aerial surveying.
It is a proof of concept written during an internship at GPSit in Tauranga.

## Background

GPSit runs aerial surveying of large farmland by flying an aeroplane over the area and taking photos at predetirmined points over the flight path.
The plane is leased and has a semi-permanent camera pod mounted under the wing.
In addition to taking photos a technician needed to access the farmland in order to calibrate the gps readings to the photos.

## The Problem

Access can be limited on some farmland and therefore the technician is not able to get to the best calibration spots.
The camera pod can only be removed and installed by a flight engineer due to wiring for power and triggering the camera.

## The Project

My research project was to find a wireless camera pod solution so that the pod could be removed and installed without the need for a flight engineer, which would allow GPSit to afix the pod on planes in other areas such as the south island and overseas.
In addition to this the surveying should be accurate without need for a technician on te ground.

## Accurate GPS w/o a Technician

In my research I found that PPP-RTK gps would allow for centimeter accuracy (<2cm) which was within error margins for the use case.
Unfortunately I was unable to test this practically as units and subscriptions were expensive (for an intern project).
I produced a report discussing the technology, accuracy of the photos it could produce, and how it could save the company thousands of dollars in the medium term.

## Wireless Camera Pod

The other part of my research was to find a camera pod system that could hold a Canon DSLR 90D, GPS module, and batteries; be attachable without a flight engineer; and be able tooperate wirelessly from inside the cockpit.
There were a few camera pod systems that I discovered would fit the requirements, however they were all prohibitively expensive and either required subscriptions or engineers to be flown over from Europe.
As an alternative to ready made systems I found that we could fit the required electronics in a much cheaper enclosure and produced this proof of concept for the wireless control of the device.

## The Code

This program allows the user to map out an area using polygons, generate flight lines with the given parameters, and creates camera trigger points.
The user then flys along the flight lines and when over a camera trigger sends a signal to the camera wirelessly. It requires a gps input and Canon wireless trigger module.
