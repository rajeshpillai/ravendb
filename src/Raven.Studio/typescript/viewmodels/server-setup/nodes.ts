import setupStep = require("viewmodels/server-setup/setupStep");
import router = require("plugins/router");
import nodeInfo = require("models/setup/nodeInfo");
import loadAgreementCommand = require("commands/setup/loadAgreementCommand");

import serverSetup = require("models/setup/serverSetup");

class nodes extends setupStep {

    currentStep: number;

    agreementUrl = ko.observable<string>();
    confirmation = ko.observable<boolean>(false);
    confirmationValidationGroup = ko.validatedObservable({
        confirmation: this.confirmation
    });
    
    editedNode = ko.observable<nodeInfo>();
    
    defineServerUrl: KnockoutComputed<boolean>;
    showDnsInfo: KnockoutComputed<boolean>;
    provideCertificates: KnockoutComputed<boolean>;
    showAgreement: KnockoutComputed<boolean>;
    showFullDomain: KnockoutComputed<boolean>;
    
    constructor() {
        super();
        
        this.bindToCurrentInstance("removeNode", "editNode");

        this.confirmation.extend({
            validation: [
                {
                    validator: (val: boolean) => val === true,
                    message: "You must accept Let's Encrypt Subscriber Agreement"
                }
            ]
        });
        
        this.defineServerUrl = ko.pureComputed(() => {
            return this.model.mode() === "Secured" && !this.model.certificate().wildcardCertificate();
        });
        
        this.provideCertificates = ko.pureComputed(() => {
            const mode = this.model.mode();
            return mode && mode === "Secured";
        });

        this.showDnsInfo = ko.pureComputed(() => this.model.mode() === "LetsEncrypt");
        this.showFullDomain = ko.pureComputed(() => this.model.mode() == "LetsEncrypt");
        this.showAgreement = ko.pureComputed(() => this.model.mode() == "LetsEncrypt");
    }

    canActivate(): JQueryPromise<canActivateResultDto> {
        const mode = this.model.mode();

        if (mode && (mode === "Secured" || mode === "LetsEncrypt")) {
            return $.when({ can: true });
        }

        return $.when({ redirect: "#welcome" });
    }
    
    activate(args: any) {
        super.activate(args);

        switch (this.model.mode()) {
            case "LetsEncrypt":
                this.currentStep = 4;
                break;
            case "Secured":
                this.currentStep = 3;
                break;
        }
        
        if (this.showAgreement()) {
            return new loadAgreementCommand(this.model.domain().userEmail())
                .execute()
                .done(url => {
                    this.agreementUrl(url);
                });
        }
    }
    
    compositionComplete() {
        super.compositionComplete();
        
        if (this.model.nodes().length) {
            this.editedNode(this.model.nodes()[0]);
        }
    }
    
    save() {
        const nodes = this.model.nodes();
        let isValid = true;
        
        if (this.showAgreement()) {
            if (!this.isValid(this.confirmationValidationGroup)) {
                isValid = false;
            }
        }
        
        nodes.forEach(node => {
            if (!this.isValid(node.validationGroup)) {
                isValid = false;
            }
            
            node.ips().forEach(entry => {
                if (!this.isValid(entry.validationGroup)) {
                    isValid = false;
                }
            });
        });
        
        if (!this.isValid(this.model.nodesValidationGroup)) {
            isValid = false;
        }
        
        if (isValid) {
            router.navigate("#finish");
        }
    }

    back() {
        switch (this.model.mode()) {
            case "LetsEncrypt":
                router.navigate("#domain");
                break;
            case "Secured":
                router.navigate("#certificate");
                break;
            default:
                router.navigate("#welcome");
        }
    }
  
    addNode() {
        const node = new nodeInfo(this.model.hostnameIsNotRequired);
        this.model.nodes.push(node);
        this.editedNode(node);
        this.updateNodeTags();
    }

    editNode(node: nodeInfo) {
        this.editedNode(node);
    }
    
    removeNode(node: nodeInfo) {
        this.model.nodes.remove(node);
        if (this.editedNode() === node) {
            this.editedNode(null);
        }
        
        this.updateNodeTags();
    }
    
    updateNodeTags() {
        let idx = 0;
        this.model.nodes().forEach(node => {
           node.nodeTag(serverSetup.nodesTags[idx]);
           idx++;
        });
    }

}

export = nodes;