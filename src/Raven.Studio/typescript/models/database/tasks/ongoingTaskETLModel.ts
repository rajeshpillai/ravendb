﻿/// <reference path="../../../../typings/tsd.d.ts"/>

import ongoingTask = require("models/database/tasks/ongoingTaskModel"); 

class ongoingTaskETLModel extends ongoingTask {

    apiKey = ko.observable<string>();
    destinationDB = ko.observable<string>();
    destinationURL = ko.observable<string>();
    destDBText: KnockoutComputed<string>;

    // Todo: Add support for the collections scripts dictionary

    constructor(dto: Raven.Server.Web.System.OngoingRavenEtl) {
        super(dto);
        this.initializeObservables();
        this.update(dto);
    }

    initializeObservables() {
        super.initializeObservables();
        
        this.destDBText = ko.pureComputed(() => {
            return `(${this.destinationDB()})`;
        });
    }

    update(dto: Raven.Server.Web.System.OngoingRavenEtl) {
        super.update(dto);
        this.destinationDB(dto.DestinationDatabase);
        this.destinationURL(dto.DestinationUrl);
    }

    enableTask() {
        alert("enabling task etl");
        // ...
    }

    disableTask() {
        alert("disabling task etl");
        // ...
    }

    editTask() {
        alert("edit task etl");
        // ...
    }

    removeTask() {
        alert("remove task etl");
        // ...
    }
}

export = ongoingTaskETLModel;
