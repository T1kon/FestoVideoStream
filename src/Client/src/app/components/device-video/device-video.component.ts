import { Component, OnInit, ViewChild } from "@angular/core";
import { BitrateOption, VgAPI } from 'videogular2/core';
import { Subscription, timer } from "rxjs";
import { IDRMLicenseServer } from 'videogular2/streaming';
import { VgDASH } from 'videogular2/src/streaming/vg-dash/vg-dash';
import { VgHLS } from 'videogular2/src/streaming/vg-hls/vg-hls';

export interface IMediaStream {
  type: 'dash';
  source: string;
  label: string;
}

@Component({
  selector: 'app-device-video',
  templateUrl: './device-video.component.html',
  styleUrls: ['./device-video.component.css']
})
export class DeviceVideoComponent implements OnInit {
  @ViewChild(VgDASH) vgDash: VgDASH;
  @ViewChild(VgHLS) vgHls: VgHLS;

  currentStream: IMediaStream;
  api: VgAPI;

  streams: IMediaStream = 
    {
      type: 'dash',
      label: 'DASH: Live Streaming',
      source: 'https://irtdashreference-i.akamaihd.net/dash/live/901161/bfs/manifestBR.mpd'
    };

  constructor() {
  }

  onPlayerReady(api: VgAPI) {
    this.api = api;
  }

  ngOnInit() {
    this.currentStream = this.streams;
  }
}