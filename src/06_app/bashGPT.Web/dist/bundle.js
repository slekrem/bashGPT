var Pr=Object.defineProperty;var Dr=(t,e,n)=>e in t?Pr(t,e,{enumerable:!0,configurable:!0,writable:!0,value:n}):t[e]=n;var V=(t,e,n)=>Dr(t,typeof e!="symbol"?e+"":e,n);(function(){const e=document.createElement("link").relList;if(e&&e.supports&&e.supports("modulepreload"))return;for(const s of document.querySelectorAll('link[rel="modulepreload"]'))i(s);new MutationObserver(s=>{for(const a of s)if(a.type==="childList")for(const r of a.addedNodes)r.tagName==="LINK"&&r.rel==="modulepreload"&&i(r)}).observe(document,{childList:!0,subtree:!0});function n(s){const a={};return s.integrity&&(a.integrity=s.integrity),s.referrerPolicy&&(a.referrerPolicy=s.referrerPolicy),s.crossOrigin==="use-credentials"?a.credentials="include":s.crossOrigin==="anonymous"?a.credentials="omit":a.credentials="same-origin",a}function i(s){if(s.ep)return;s.ep=!0;const a=n(s);fetch(s.href,a)}})();const qn=globalThis,Ns=qn.ShadowRoot&&(qn.ShadyCSS===void 0||qn.ShadyCSS.nativeShadow)&&"adoptedStyleSheets"in Document.prototype&&"replace"in CSSStyleSheet.prototype,Is=Symbol(),Js=new WeakMap;let Mi=class{constructor(e,n,i){if(this._$cssResult$=!0,i!==Is)throw Error("CSSResult is not constructable. Use `unsafeCSS` or `css` instead.");this.cssText=e,this.t=n}get styleSheet(){let e=this.o;const n=this.t;if(Ns&&e===void 0){const i=n!==void 0&&n.length===1;i&&(e=Js.get(n)),e===void 0&&((this.o=e=new CSSStyleSheet).replaceSync(this.cssText),i&&Js.set(n,e))}return e}toString(){return this.cssText}};const Li=t=>new Mi(typeof t=="string"?t:t+"",void 0,Is),Ke=(t,...e)=>{const n=t.length===1?t[0]:e.reduce((i,s,a)=>i+(r=>{if(r._$cssResult$===!0)return r.cssText;if(typeof r=="number")return r;throw Error("Value passed to 'css' function must be a 'css' function result: "+r+". Use 'unsafeCSS' to pass non-literal values, but take care to ensure page security.")})(s)+t[a+1],t[0]);return new Mi(n,t,Is)},zr=(t,e)=>{if(Ns)t.adoptedStyleSheets=e.map(n=>n instanceof CSSStyleSheet?n:n.styleSheet);else for(const n of e){const i=document.createElement("style"),s=qn.litNonce;s!==void 0&&i.setAttribute("nonce",s),i.textContent=n.cssText,t.appendChild(i)}},ei=Ns?t=>t:t=>t instanceof CSSStyleSheet?(e=>{let n="";for(const i of e.cssRules)n+=i.cssText;return Li(n)})(t):t;const{is:Ur,defineProperty:Br,getOwnPropertyDescriptor:Hr,getOwnPropertyNames:Fr,getOwnPropertySymbols:Gr,getPrototypeOf:Wr}=Object,ht=globalThis,ti=ht.trustedTypes,jr=ti?ti.emptyScript:"",qr=ht.reactiveElementPolyfillSupport,fn=(t,e)=>t,Vn={toAttribute(t,e){switch(e){case Boolean:t=t?jr:null;break;case Object:case Array:t=t==null?t:JSON.stringify(t)}return t},fromAttribute(t,e){let n=t;switch(e){case Boolean:n=t!==null;break;case Number:n=t===null?null:Number(t);break;case Object:case Array:try{n=JSON.parse(t)}catch{n=null}}return n}},Ms=(t,e)=>!Ur(t,e),ni={attribute:!0,type:String,converter:Vn,reflect:!1,useDefault:!1,hasChanged:Ms};Symbol.metadata??(Symbol.metadata=Symbol("metadata")),ht.litPropertyMetadata??(ht.litPropertyMetadata=new WeakMap);let Gt=class extends HTMLElement{static addInitializer(e){this._$Ei(),(this.l??(this.l=[])).push(e)}static get observedAttributes(){return this.finalize(),this._$Eh&&[...this._$Eh.keys()]}static createProperty(e,n=ni){if(n.state&&(n.attribute=!1),this._$Ei(),this.prototype.hasOwnProperty(e)&&((n=Object.create(n)).wrapped=!0),this.elementProperties.set(e,n),!n.noAccessor){const i=Symbol(),s=this.getPropertyDescriptor(e,i,n);s!==void 0&&Br(this.prototype,e,s)}}static getPropertyDescriptor(e,n,i){const{get:s,set:a}=Hr(this.prototype,e)??{get(){return this[n]},set(r){this[n]=r}};return{get:s,set(r){const d=s?.call(this);a?.call(this,r),this.requestUpdate(e,d,i)},configurable:!0,enumerable:!0}}static getPropertyOptions(e){return this.elementProperties.get(e)??ni}static _$Ei(){if(this.hasOwnProperty(fn("elementProperties")))return;const e=Wr(this);e.finalize(),e.l!==void 0&&(this.l=[...e.l]),this.elementProperties=new Map(e.elementProperties)}static finalize(){if(this.hasOwnProperty(fn("finalized")))return;if(this.finalized=!0,this._$Ei(),this.hasOwnProperty(fn("properties"))){const n=this.properties,i=[...Fr(n),...Gr(n)];for(const s of i)this.createProperty(s,n[s])}const e=this[Symbol.metadata];if(e!==null){const n=litPropertyMetadata.get(e);if(n!==void 0)for(const[i,s]of n)this.elementProperties.set(i,s)}this._$Eh=new Map;for(const[n,i]of this.elementProperties){const s=this._$Eu(n,i);s!==void 0&&this._$Eh.set(s,n)}this.elementStyles=this.finalizeStyles(this.styles)}static finalizeStyles(e){const n=[];if(Array.isArray(e)){const i=new Set(e.flat(1/0).reverse());for(const s of i)n.unshift(ei(s))}else e!==void 0&&n.push(ei(e));return n}static _$Eu(e,n){const i=n.attribute;return i===!1?void 0:typeof i=="string"?i:typeof e=="string"?e.toLowerCase():void 0}constructor(){super(),this._$Ep=void 0,this.isUpdatePending=!1,this.hasUpdated=!1,this._$Em=null,this._$Ev()}_$Ev(){this._$ES=new Promise(e=>this.enableUpdating=e),this._$AL=new Map,this._$E_(),this.requestUpdate(),this.constructor.l?.forEach(e=>e(this))}addController(e){(this._$EO??(this._$EO=new Set)).add(e),this.renderRoot!==void 0&&this.isConnected&&e.hostConnected?.()}removeController(e){this._$EO?.delete(e)}_$E_(){const e=new Map,n=this.constructor.elementProperties;for(const i of n.keys())this.hasOwnProperty(i)&&(e.set(i,this[i]),delete this[i]);e.size>0&&(this._$Ep=e)}createRenderRoot(){const e=this.shadowRoot??this.attachShadow(this.constructor.shadowRootOptions);return zr(e,this.constructor.elementStyles),e}connectedCallback(){this.renderRoot??(this.renderRoot=this.createRenderRoot()),this.enableUpdating(!0),this._$EO?.forEach(e=>e.hostConnected?.())}enableUpdating(e){}disconnectedCallback(){this._$EO?.forEach(e=>e.hostDisconnected?.())}attributeChangedCallback(e,n,i){this._$AK(e,i)}_$ET(e,n){const i=this.constructor.elementProperties.get(e),s=this.constructor._$Eu(e,i);if(s!==void 0&&i.reflect===!0){const a=(i.converter?.toAttribute!==void 0?i.converter:Vn).toAttribute(n,i.type);this._$Em=e,a==null?this.removeAttribute(s):this.setAttribute(s,a),this._$Em=null}}_$AK(e,n){const i=this.constructor,s=i._$Eh.get(e);if(s!==void 0&&this._$Em!==s){const a=i.getPropertyOptions(s),r=typeof a.converter=="function"?{fromAttribute:a.converter}:a.converter?.fromAttribute!==void 0?a.converter:Vn;this._$Em=s;const d=r.fromAttribute(n,a.type);this[s]=d??this._$Ej?.get(s)??d,this._$Em=null}}requestUpdate(e,n,i,s=!1,a){if(e!==void 0){const r=this.constructor;if(s===!1&&(a=this[e]),i??(i=r.getPropertyOptions(e)),!((i.hasChanged??Ms)(a,n)||i.useDefault&&i.reflect&&a===this._$Ej?.get(e)&&!this.hasAttribute(r._$Eu(e,i))))return;this.C(e,n,i)}this.isUpdatePending===!1&&(this._$ES=this._$EP())}C(e,n,{useDefault:i,reflect:s,wrapped:a},r){i&&!(this._$Ej??(this._$Ej=new Map)).has(e)&&(this._$Ej.set(e,r??n??this[e]),a!==!0||r!==void 0)||(this._$AL.has(e)||(this.hasUpdated||i||(n=void 0),this._$AL.set(e,n)),s===!0&&this._$Em!==e&&(this._$Eq??(this._$Eq=new Set)).add(e))}async _$EP(){this.isUpdatePending=!0;try{await this._$ES}catch(n){Promise.reject(n)}const e=this.scheduleUpdate();return e!=null&&await e,!this.isUpdatePending}scheduleUpdate(){return this.performUpdate()}performUpdate(){if(!this.isUpdatePending)return;if(!this.hasUpdated){if(this.renderRoot??(this.renderRoot=this.createRenderRoot()),this._$Ep){for(const[s,a]of this._$Ep)this[s]=a;this._$Ep=void 0}const i=this.constructor.elementProperties;if(i.size>0)for(const[s,a]of i){const{wrapped:r}=a,d=this[s];r!==!0||this._$AL.has(s)||d===void 0||this.C(s,void 0,a,d)}}let e=!1;const n=this._$AL;try{e=this.shouldUpdate(n),e?(this.willUpdate(n),this._$EO?.forEach(i=>i.hostUpdate?.()),this.update(n)):this._$EM()}catch(i){throw e=!1,this._$EM(),i}e&&this._$AE(n)}willUpdate(e){}_$AE(e){this._$EO?.forEach(n=>n.hostUpdated?.()),this.hasUpdated||(this.hasUpdated=!0,this.firstUpdated(e)),this.updated(e)}_$EM(){this._$AL=new Map,this.isUpdatePending=!1}get updateComplete(){return this.getUpdateComplete()}getUpdateComplete(){return this._$ES}shouldUpdate(e){return!0}update(e){this._$Eq&&(this._$Eq=this._$Eq.forEach(n=>this._$ET(n,this[n]))),this._$EM()}updated(e){}firstUpdated(e){}};Gt.elementStyles=[],Gt.shadowRootOptions={mode:"open"},Gt[fn("elementProperties")]=new Map,Gt[fn("finalized")]=new Map,qr?.({ReactiveElement:Gt}),(ht.reactiveElementVersions??(ht.reactiveElementVersions=[])).push("2.1.2");const bn=globalThis,si=t=>t,Xn=bn.trustedTypes,ii=Xn?Xn.createPolicy("lit-html",{createHTML:t=>t}):void 0,Pi="$lit$",ut=`lit$${Math.random().toFixed(9).slice(2)}$`,Di="?"+ut,Kr=`<${Di}>`,Rt=document,_n=()=>Rt.createComment(""),xn=t=>t===null||typeof t!="object"&&typeof t!="function",Ls=Array.isArray,Zr=t=>Ls(t)||typeof t?.[Symbol.iterator]=="function",bs=`[ 	
\f\r]`,sn=/<(?:(!--|\/[^a-zA-Z])|(\/?[a-zA-Z][^>\s]*)|(\/?$))/g,ri=/-->/g,ai=/>/g,St=RegExp(`>|${bs}(?:([^\\s"'>=/]+)(${bs}*=${bs}*(?:[^ 	
\f\r"'\`<>=]|("|')|))|$)`,"g"),oi=/'/g,li=/"/g,zi=/^(?:script|style|textarea|title)$/i,Vr=t=>(e,...n)=>({_$litType$:t,strings:e,values:n}),T=Vr(1),gt=Symbol.for("lit-noChange"),oe=Symbol.for("lit-nothing"),ci=new WeakMap,$t=Rt.createTreeWalker(Rt,129);function Ui(t,e){if(!Ls(t)||!t.hasOwnProperty("raw"))throw Error("invalid template strings array");return ii!==void 0?ii.createHTML(e):e}const Xr=(t,e)=>{const n=t.length-1,i=[];let s,a=e===2?"<svg>":e===3?"<math>":"",r=sn;for(let d=0;d<n;d++){const o=t[d];let p,u,g=-1,m=0;for(;m<o.length&&(r.lastIndex=m,u=r.exec(o),u!==null);)m=r.lastIndex,r===sn?u[1]==="!--"?r=ri:u[1]!==void 0?r=ai:u[2]!==void 0?(zi.test(u[2])&&(s=RegExp("</"+u[2],"g")),r=St):u[3]!==void 0&&(r=St):r===St?u[0]===">"?(r=s??sn,g=-1):u[1]===void 0?g=-2:(g=r.lastIndex-u[2].length,p=u[1],r=u[3]===void 0?St:u[3]==='"'?li:oi):r===li||r===oi?r=St:r===ri||r===ai?r=sn:(r=St,s=void 0);const y=r===St&&t[d+1].startsWith("/>")?" ":"";a+=r===sn?o+Kr:g>=0?(i.push(p),o.slice(0,g)+Pi+o.slice(g)+ut+y):o+ut+(g===-2?d:y)}return[Ui(t,a+(t[n]||"<?>")+(e===2?"</svg>":e===3?"</math>":"")),i]};class yn{constructor({strings:e,_$litType$:n},i){let s;this.parts=[];let a=0,r=0;const d=e.length-1,o=this.parts,[p,u]=Xr(e,n);if(this.el=yn.createElement(p,i),$t.currentNode=this.el.content,n===2||n===3){const g=this.el.content.firstChild;g.replaceWith(...g.childNodes)}for(;(s=$t.nextNode())!==null&&o.length<d;){if(s.nodeType===1){if(s.hasAttributes())for(const g of s.getAttributeNames())if(g.endsWith(Pi)){const m=u[r++],y=s.getAttribute(g).split(ut),E=/([.?@])?(.*)/.exec(m);o.push({type:1,index:a,name:E[2],strings:y,ctor:E[1]==="."?Qr:E[1]==="?"?Jr:E[1]==="@"?ea:ns}),s.removeAttribute(g)}else g.startsWith(ut)&&(o.push({type:6,index:a}),s.removeAttribute(g));if(zi.test(s.tagName)){const g=s.textContent.split(ut),m=g.length-1;if(m>0){s.textContent=Xn?Xn.emptyScript:"";for(let y=0;y<m;y++)s.append(g[y],_n()),$t.nextNode(),o.push({type:2,index:++a});s.append(g[m],_n())}}}else if(s.nodeType===8)if(s.data===Di)o.push({type:2,index:a});else{let g=-1;for(;(g=s.data.indexOf(ut,g+1))!==-1;)o.push({type:7,index:a}),g+=ut.length-1}a++}}static createElement(e,n){const i=Rt.createElement("template");return i.innerHTML=e,i}}function Wt(t,e,n=t,i){if(e===gt)return e;let s=i!==void 0?n._$Co?.[i]:n._$Cl;const a=xn(e)?void 0:e._$litDirective$;return s?.constructor!==a&&(s?._$AO?.(!1),a===void 0?s=void 0:(s=new a(t),s._$AT(t,n,i)),i!==void 0?(n._$Co??(n._$Co=[]))[i]=s:n._$Cl=s),s!==void 0&&(e=Wt(t,s._$AS(t,e.values),s,i)),e}class Yr{constructor(e,n){this._$AV=[],this._$AN=void 0,this._$AD=e,this._$AM=n}get parentNode(){return this._$AM.parentNode}get _$AU(){return this._$AM._$AU}u(e){const{el:{content:n},parts:i}=this._$AD,s=(e?.creationScope??Rt).importNode(n,!0);$t.currentNode=s;let a=$t.nextNode(),r=0,d=0,o=i[0];for(;o!==void 0;){if(r===o.index){let p;o.type===2?p=new ts(a,a.nextSibling,this,e):o.type===1?p=new o.ctor(a,o.name,o.strings,this,e):o.type===6&&(p=new ta(a,this,e)),this._$AV.push(p),o=i[++d]}r!==o?.index&&(a=$t.nextNode(),r++)}return $t.currentNode=Rt,s}p(e){let n=0;for(const i of this._$AV)i!==void 0&&(i.strings!==void 0?(i._$AI(e,i,n),n+=i.strings.length-2):i._$AI(e[n])),n++}}let ts=class Bi{get _$AU(){return this._$AM?._$AU??this._$Cv}constructor(e,n,i,s){this.type=2,this._$AH=oe,this._$AN=void 0,this._$AA=e,this._$AB=n,this._$AM=i,this.options=s,this._$Cv=s?.isConnected??!0}get parentNode(){let e=this._$AA.parentNode;const n=this._$AM;return n!==void 0&&e?.nodeType===11&&(e=n.parentNode),e}get startNode(){return this._$AA}get endNode(){return this._$AB}_$AI(e,n=this){e=Wt(this,e,n),xn(e)?e===oe||e==null||e===""?(this._$AH!==oe&&this._$AR(),this._$AH=oe):e!==this._$AH&&e!==gt&&this._(e):e._$litType$!==void 0?this.$(e):e.nodeType!==void 0?this.T(e):Zr(e)?this.k(e):this._(e)}O(e){return this._$AA.parentNode.insertBefore(e,this._$AB)}T(e){this._$AH!==e&&(this._$AR(),this._$AH=this.O(e))}_(e){this._$AH!==oe&&xn(this._$AH)?this._$AA.nextSibling.data=e:this.T(Rt.createTextNode(e)),this._$AH=e}$(e){const{values:n,_$litType$:i}=e,s=typeof i=="number"?this._$AC(e):(i.el===void 0&&(i.el=yn.createElement(Ui(i.h,i.h[0]),this.options)),i);if(this._$AH?._$AD===s)this._$AH.p(n);else{const a=new Yr(s,this),r=a.u(this.options);a.p(n),this.T(r),this._$AH=a}}_$AC(e){let n=ci.get(e.strings);return n===void 0&&ci.set(e.strings,n=new yn(e)),n}k(e){Ls(this._$AH)||(this._$AH=[],this._$AR());const n=this._$AH;let i,s=0;for(const a of e)s===n.length?n.push(i=new Bi(this.O(_n()),this.O(_n()),this,this.options)):i=n[s],i._$AI(a),s++;s<n.length&&(this._$AR(i&&i._$AB.nextSibling,s),n.length=s)}_$AR(e=this._$AA.nextSibling,n){for(this._$AP?.(!1,!0,n);e!==this._$AB;){const i=si(e).nextSibling;si(e).remove(),e=i}}setConnected(e){this._$AM===void 0&&(this._$Cv=e,this._$AP?.(e))}},ns=class{get tagName(){return this.element.tagName}get _$AU(){return this._$AM._$AU}constructor(e,n,i,s,a){this.type=1,this._$AH=oe,this._$AN=void 0,this.element=e,this.name=n,this._$AM=s,this.options=a,i.length>2||i[0]!==""||i[1]!==""?(this._$AH=Array(i.length-1).fill(new String),this.strings=i):this._$AH=oe}_$AI(e,n=this,i,s){const a=this.strings;let r=!1;if(a===void 0)e=Wt(this,e,n,0),r=!xn(e)||e!==this._$AH&&e!==gt,r&&(this._$AH=e);else{const d=e;let o,p;for(e=a[0],o=0;o<a.length-1;o++)p=Wt(this,d[i+o],n,o),p===gt&&(p=this._$AH[o]),r||(r=!xn(p)||p!==this._$AH[o]),p===oe?e=oe:e!==oe&&(e+=(p??"")+a[o+1]),this._$AH[o]=p}r&&!s&&this.j(e)}j(e){e===oe?this.element.removeAttribute(this.name):this.element.setAttribute(this.name,e??"")}},Qr=class extends ns{constructor(){super(...arguments),this.type=3}j(e){this.element[this.name]=e===oe?void 0:e}},Jr=class extends ns{constructor(){super(...arguments),this.type=4}j(e){this.element.toggleAttribute(this.name,!!e&&e!==oe)}},ea=class extends ns{constructor(e,n,i,s,a){super(e,n,i,s,a),this.type=5}_$AI(e,n=this){if((e=Wt(this,e,n,0)??oe)===gt)return;const i=this._$AH,s=e===oe&&i!==oe||e.capture!==i.capture||e.once!==i.once||e.passive!==i.passive,a=e!==oe&&(i===oe||s);s&&this.element.removeEventListener(this.name,this,i),a&&this.element.addEventListener(this.name,this,e),this._$AH=e}handleEvent(e){typeof this._$AH=="function"?this._$AH.call(this.options?.host??this.element,e):this._$AH.handleEvent(e)}},ta=class{constructor(e,n,i){this.element=e,this.type=6,this._$AN=void 0,this._$AM=n,this.options=i}get _$AU(){return this._$AM._$AU}_$AI(e){Wt(this,e)}};const na={I:ts},sa=bn.litHtmlPolyfillSupport;sa?.(yn,ts),(bn.litHtmlVersions??(bn.litHtmlVersions=[])).push("3.3.2");const ia=(t,e,n)=>{const i=n?.renderBefore??e;let s=i._$litPart$;if(s===void 0){const a=n?.renderBefore??null;i._$litPart$=s=new ts(e.insertBefore(_n(),a),a,void 0,n??{})}return s._$AI(t),s};const mn=globalThis;let Te=class extends Gt{constructor(){super(...arguments),this.renderOptions={host:this},this._$Do=void 0}createRenderRoot(){var n;const e=super.createRenderRoot();return(n=this.renderOptions).renderBefore??(n.renderBefore=e.firstChild),e}update(e){const n=this.render();this.hasUpdated||(this.renderOptions.isConnected=this.isConnected),super.update(e),this._$Do=ia(n,this.renderRoot,this.renderOptions)}connectedCallback(){super.connectedCallback(),this._$Do?.setConnected(!0)}disconnectedCallback(){super.disconnectedCallback(),this._$Do?.setConnected(!1)}render(){return gt}};Te._$litElement$=!0,Te.finalized=!0,mn.litElementHydrateSupport?.({LitElement:Te});const ra=mn.litElementPolyfillSupport;ra?.({LitElement:Te});(mn.litElementVersions??(mn.litElementVersions=[])).push("4.2.2");const Ze=t=>(e,n)=>{n!==void 0?n.addInitializer(()=>{customElements.define(t,e)}):customElements.define(t,e)};const aa={attribute:!0,type:String,converter:Vn,reflect:!1,hasChanged:Ms},oa=(t=aa,e,n)=>{const{kind:i,metadata:s}=n;let a=globalThis.litPropertyMetadata.get(s);if(a===void 0&&globalThis.litPropertyMetadata.set(s,a=new Map),i==="setter"&&((t=Object.create(t)).wrapped=!0),a.set(n.name,t),i==="accessor"){const{name:r}=n;return{set(d){const o=e.get.call(this);e.set.call(this,d),this.requestUpdate(r,o,t,!0,d)},init(d){return d!==void 0&&this.C(r,void 0,t,d),d}}}if(i==="setter"){const{name:r}=n;return function(d){const o=this[r];e.call(this,d),this.requestUpdate(r,o,t,!0,d)}}throw Error("Unsupported decorator location: "+i)};function fe(t){return(e,n)=>typeof n=="object"?oa(t,e,n):((i,s,a)=>{const r=s.hasOwnProperty(a);return s.constructor.createProperty(a,i),r?Object.getOwnPropertyDescriptor(s,a):void 0})(t,e,n)}function W(t){return fe({...t,state:!0,attribute:!1})}const Hi={CHILD:2},Fi=t=>(...e)=>({_$litDirective$:t,values:e});let Gi=class{constructor(e){}get _$AU(){return this._$AM._$AU}_$AT(e,n,i){this._$Ct=e,this._$AM=n,this._$Ci=i}_$AS(e,n){return this.update(e,n)}update(e,n){return this.render(...n)}};const{I:la}=na,di=t=>t,pi=()=>document.createComment(""),rn=(t,e,n)=>{const i=t._$AA.parentNode,s=e===void 0?t._$AB:e._$AA;if(n===void 0){const a=i.insertBefore(pi(),s),r=i.insertBefore(pi(),s);n=new la(a,r,t,t.options)}else{const a=n._$AB.nextSibling,r=n._$AM,d=r!==t;if(d){let o;n._$AQ?.(t),n._$AM=t,n._$AP!==void 0&&(o=t._$AU)!==r._$AU&&n._$AP(o)}if(a!==s||d){let o=n._$AA;for(;o!==a;){const p=di(o).nextSibling;di(i).insertBefore(o,s),o=p}}}return n},Tt=(t,e,n=t)=>(t._$AI(e,n),t),ca={},da=(t,e=ca)=>t._$AH=e,pa=t=>t._$AH,ms=t=>{t._$AR(),t._$AA.remove()};const ui=(t,e,n)=>{const i=new Map;for(let s=e;s<=n;s++)i.set(t[s],s);return i},vn=Fi(class extends Gi{constructor(t){if(super(t),t.type!==Hi.CHILD)throw Error("repeat() can only be used in text expressions")}dt(t,e,n){let i;n===void 0?n=e:e!==void 0&&(i=e);const s=[],a=[];let r=0;for(const d of t)s[r]=i?i(d,r):r,a[r]=n(d,r),r++;return{values:a,keys:s}}render(t,e,n){return this.dt(t,e,n).values}update(t,[e,n,i]){const s=pa(t),{values:a,keys:r}=this.dt(e,n,i);if(!Array.isArray(s))return this.ut=r,a;const d=this.ut??(this.ut=[]),o=[];let p,u,g=0,m=s.length-1,y=0,E=a.length-1;for(;g<=m&&y<=E;)if(s[g]===null)g++;else if(s[m]===null)m--;else if(d[g]===r[y])o[y]=Tt(s[g],a[y]),g++,y++;else if(d[m]===r[E])o[E]=Tt(s[m],a[E]),m--,E--;else if(d[g]===r[E])o[E]=Tt(s[g],a[E]),rn(t,o[E+1],s[g]),g++,E--;else if(d[m]===r[y])o[y]=Tt(s[m],a[y]),rn(t,s[g],s[m]),m--,y++;else if(p===void 0&&(p=ui(r,y,E),u=ui(d,g,m)),p.has(d[g]))if(p.has(d[m])){const R=u.get(r[y]),B=R!==void 0?s[R]:null;if(B===null){const j=rn(t,s[g]);Tt(j,a[y]),o[y]=j}else o[y]=Tt(B,a[y]),rn(t,s[g],B),s[R]=null;y++}else ms(s[m]),m--;else ms(s[g]),g++;for(;y<=E;){const R=rn(t,o[E+1]);Tt(R,a[y]),o[y++]=R}for(;g<=m;){const R=s[g++];R!==null&&ms(R)}return this.ut=r,da(t,o),gt}});var ua=Object.defineProperty,ha=Object.getOwnPropertyDescriptor,ss=(t,e,n,i)=>{for(var s=i>1?void 0:i?ha(e,n):e,a=t.length-1,r;a>=0;a--)(r=t[a])&&(s=(i?r(e,n,s):r(s))||s);return i&&s&&ua(e,n,s),s};let jt=class extends Te{constructor(){super(...arguments),this.view="dashboard",this.sessions=[],this.activeSessionId=null}_dispatch(t,e){this.dispatchEvent(new CustomEvent(t,{detail:e,bubbles:!0,composed:!0}))}_formatDate(t){try{return new Date(t).toLocaleDateString("en-US",{day:"2-digit",month:"short"})}catch{return""}}render(){return T`
      <button
        class="new-chat-btn"
        @click=${()=>this._dispatch("new-chat")}
      >
        + New chat
      </button>

      <div class="section-label">History</div>

      <div class="session-list">
        ${this.sessions.length===0?T`<div class="empty-sessions">No sessions yet</div>`:vn(this.sessions,t=>t.id,t=>T`
                <button
                  class="session-item ${t.id===this.activeSessionId?"active":""}"
                  @click=${()=>this._dispatch("session-select",{id:t.id})}
                  aria-current=${t.id===this.activeSessionId?"page":"false"}
                >
                  <div class="session-title">${t.title}</div>
                  <div class="session-date">${this._formatDate(t.createdAt)}</div>
                </button>
              `)}
      </div>

      <div class="divider"></div>

      <button
        class="nav-btn ${this.view==="agents"?"active":""}"
        @click=${()=>this._dispatch("view-change",{view:"agents"})}
      >
        <span class="icon">AI</span> Agents
      </button>

      <button
        class="nav-btn ${this.view==="tools"?"active":""}"
        @click=${()=>this._dispatch("view-change",{view:"tools"})}
      >
        <span class="icon">T</span> Tools
      </button>

      <button
        class="nav-btn ${this.view==="settings"?"active":""}"
        @click=${()=>this._dispatch("view-change",{view:"settings"})}
        style="margin-bottom: 12px;"
      >
        <span class="icon">S</span> Settings
      </button>
    `}};jt.styles=Ke`
    :host {
      display: flex;
      flex-direction: column;
      width: var(--sidebar-width, 220px);
      background: rgba(11, 17, 32, 0.95);
      border-right: 1px solid #1e293b;
      flex-shrink: 0;
      overflow: hidden;
    }

    .section-label {
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.08em;
      color: #475569;
      text-transform: uppercase;
      padding: 16px 14px 6px;
    }

    .session-list {
      flex: 1;
      overflow-y: auto;
      padding: 0 8px;
    }

    .session-item {
      border-radius: 8px;
      padding: 8px 10px;
      cursor: pointer;
      transition: background 0.12s;
      border: 1px solid transparent;
      background: none;
      width: 100%;
      text-align: left;
      font-family: inherit;
      color: inherit;
    }
    .session-item:hover { background: #1e293b; }
    .session-item:focus-visible {
      outline: 2px solid #22c55e;
      outline-offset: 1px;
    }
    .session-item.active {
      background: #0f2d1a;
      border-color: #166534;
    }
    .session-title {
      font-size: 13px;
      font-weight: 500;
      color: #e2e8f0;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .session-date {
      font-size: 11px;
      color: #475569;
      margin-top: 2px;
    }

    .empty-sessions {
      padding: 12px 14px;
      font-size: 12px;
      color: #475569;
      font-style: italic;
    }

    .divider {
      height: 1px;
      background: #1e293b;
      margin: 8px 14px;
    }

    .nav-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 9px 14px;
      margin: 2px 8px;
      border-radius: 8px;
      cursor: pointer;
      font-size: 13px;
      color: #94a3b8;
      background: none;
      border: none;
      width: calc(100% - 16px);
      text-align: left;
      transition: background 0.12s, color 0.12s;
    }
    .nav-btn:hover { background: #1e293b; color: #e2e8f0; }
    .nav-btn:focus-visible { outline: 2px solid #22c55e; outline-offset: 1px; }
    .nav-btn.active { background: #1e293b; color: #f1f5f9; font-weight: 600; }
    .nav-btn .icon { font-size: 15px; }

    .new-chat-btn {
      margin: 12px 8px 4px;
      padding: 8px 12px;
      border-radius: 8px;
      background: #14532d;
      border: 1px solid #16a34a;
      color: #dcfce7;
      font-size: 13px;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 6px;
      transition: background 0.12s;
    }
    .new-chat-btn:hover { background: #166534; }
    .new-chat-btn:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }
    .new-chat-btn:disabled { opacity: 0.4; cursor: not-allowed; }

    @media (max-width: 768px) {
      :host { width: 100%; border-right: none; border-bottom: 1px solid #1e293b; }
    }
  `;ss([fe()],jt.prototype,"view",2);ss([fe({type:Array})],jt.prototype,"sessions",2);ss([fe()],jt.prototype,"activeSessionId",2);jt=ss([Ze("bashgpt-sidebar")],jt);var ga=Object.defineProperty,fa=Object.getOwnPropertyDescriptor,Ps=(t,e,n,i)=>{for(var s=i>1?void 0:i?fa(e,n):e,a=t.length-1,r;a>=0;a--)(r=t[a])&&(s=(i?r(e,n,s):r(s))||s);return i&&s&&ga(e,n,s),s};const hi=[{category:"System",title:"Processes",hint:"List all running processes",risk:"safe",prompt:"Show all running processes and their resource usage."},{category:"System",title:"Memory & Disk",hint:"Show disk and memory usage",risk:"safe",prompt:"Show the current disk and memory usage."},{category:"System",title:"Uptime & Load",hint:"Check system uptime and load",risk:"safe",prompt:"How long has the system been running? Show uptime and load average."},{category:"System",title:"Logs",hint:"Show recent system logs",risk:"safe",prompt:"Show the latest system logs (last 50 lines)."},{category:"Git",title:"Status",hint:"Show the current branch and changes",risk:"safe",prompt:"Show the git status of the current directory."},{category:"Git",title:"Log",hint:"Show the last 10 commits",risk:"safe",prompt:"Show the last 10 git commits with author and date."},{category:"Files",title:"Directory",hint:"List files in the current folder",risk:"safe",prompt:"List all files in the current directory with details."},{category:"Files",title:"Find File",hint:"Search for a file by name",risk:"safe",prompt:"Search for a specific file. Which filename should I look for?"},{category:"Network",title:"IP Addresses",hint:"Show network interfaces",risk:"safe",prompt:"Show all current network interfaces and IP addresses."},{category:"Network",title:"Open Ports",hint:"List active services and ports",risk:"medium",prompt:"Which ports are currently open and which services are listening?"}],gi="bashgpt_recent_prompts",ba=3;let wn=class extends Te{constructor(){super(...arguments),this._search="",this._recent=[]}connectedCallback(){super.connectedCallback(),this._loadRecent()}_loadRecent(){try{const t=localStorage.getItem(gi);this._recent=t?JSON.parse(t):[]}catch{this._recent=[]}}_dispatch(t){try{const e=[t,...this._recent.filter(n=>n!==t)].slice(0,ba);localStorage.setItem(gi,JSON.stringify(e)),this._recent=e}catch{}this.dispatchEvent(new CustomEvent("prompt-selected",{detail:{prompt:t},bubbles:!0,composed:!0}))}get _filtered(){const t=this._search.toLowerCase().trim();return t?hi.filter(e=>e.title.toLowerCase().includes(t)||e.hint.toLowerCase().includes(t)||e.category.toLowerCase().includes(t)):hi}get _categories(){return[...new Set(this._filtered.map(t=>t.category))]}_riskLabel(t){return t==="safe"?"Safe":"Medium"}render(){return T`
      <div class="greeting">Hello! I am bashGPT.</div>
      <div class="subtitle">What would you like to get done today?</div>

      <input
        class="search"
        type="search"
        placeholder="Search use cases..."
        aria-label="Search use cases"
        .value=${this._search}
        @input=${t=>{this._search=t.target.value}}
      />

      ${this._recent.length>0?T`
        <div class="section-label">Recently used</div>
        <div class="recent-row">
          ${this._recent.map(t=>T`
            <button class="recent-chip" title=${t} @click=${()=>this._dispatch(t)}>${t}</button>
          `)}
        </div>
      `:""}

      ${this._filtered.length===0?T`
        <div class="empty-search">No use cases found for "${this._search}".</div>
      `:""}

      ${this._categories.map(t=>T`
        <div class="category-block">
          <div class="category-header">${t}</div>
          <div class="cards-row">
            ${this._filtered.filter(e=>e.category===t).map(e=>T`
              <div class="card">
                <div class="card-title">${e.title}</div>
                <div class="card-hint">${e.hint}</div>
                <span class="risk-badge risk-${e.risk}">${this._riskLabel(e.risk)}</span>
                <div class="card-actions">
                  <button class="run" @click=${()=>this._dispatch(e.prompt)}>Run</button>
                  <button @click=${()=>this.dispatchEvent(new CustomEvent("prompt-edit",{detail:{prompt:e.prompt},bubbles:!0,composed:!0}))}>Edit</button>
                </div>
              </div>
            `)}
          </div>
        </div>
      `)}
    `}};wn.styles=Ke`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow-y: auto;
      padding: 28px 28px 40px;
      box-sizing: border-box;
    }

    .greeting {
      font-size: 22px;
      font-weight: 700;
      color: #f1f5f9;
      margin-bottom: 4px;
    }
    .subtitle {
      font-size: 14px;
      color: #64748b;
      margin-bottom: 20px;
    }

    .search {
      width: 100%;
      max-width: 480px;
      padding: 10px 14px;
      background: #111827;
      border: 1px solid #374151;
      border-radius: 10px;
      color: #e5e7eb;
      font-size: 14px;
      outline: none;
      box-sizing: border-box;
      margin-bottom: 24px;
      transition: border-color 0.15s;
    }
    .search:focus { border-color: #4b5563; }
    .search:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }
    .search::placeholder { color: #4b5563; }

    .section-label {
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.08em;
      color: #475569;
      text-transform: uppercase;
      margin-bottom: 10px;
    }

    .recent-row {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-bottom: 28px;
    }
    .recent-chip {
      padding: 6px 12px;
      background: #1e293b;
      border: 1px solid #334155;
      border-radius: 999px;
      font-size: 12px;
      color: #94a3b8;
      cursor: pointer;
      transition: background 0.12s, color 0.12s;
      max-width: 220px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      font-family: inherit;
    }
    .recent-chip:hover { background: #334155; color: #e2e8f0; }
    .recent-chip:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }

    .category-block { margin-bottom: 24px; }

    .category-header {
      font-size: 13px;
      font-weight: 600;
      color: #64748b;
      margin-bottom: 10px;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .category-header::after {
      content: '';
      flex: 1;
      height: 1px;
      background: #1e293b;
    }

    .cards-row {
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
    }

    .card {
      background: #0f172a;
      border: 1px solid #1e293b;
      border-radius: 10px;
      padding: 14px;
      width: 200px;
      display: flex;
      flex-direction: column;
      gap: 6px;
      transition: border-color 0.12s;
    }
    .card:hover { border-color: #334155; }

    .empty-search {
      padding: 32px 0;
      text-align: center;
      color: #475569;
      font-size: 14px;
    }

    .card-title {
      font-size: 14px;
      font-weight: 600;
      color: #f1f5f9;
    }
    .card-hint {
      font-size: 12px;
      color: #64748b;
      flex: 1;
    }

    .risk-badge {
      align-self: flex-start;
      font-size: 10px;
      font-weight: 600;
      padding: 2px 7px;
      border-radius: 999px;
    }
    .risk-safe { background: #14532d; color: #86efac; }
    .risk-medium { background: #78350f; color: #fcd34d; }

    .card-actions {
      display: flex;
      gap: 6px;
      margin-top: 4px;
    }
    .card-actions button {
      flex: 1;
      padding: 5px 8px;
      font-size: 12px;
      border-radius: 6px;
      cursor: pointer;
      border: 1px solid #374151;
      background: #1e293b;
      color: #e5e7eb;
      transition: background 0.12s;
    }
    .card-actions button:hover { background: #334155; }
    .card-actions button:focus-visible { outline: 2px solid #22c55e; outline-offset: 1px; }
    .card-actions button.run {
      background: #14532d;
      border-color: #16a34a;
      color: #dcfce7;
    }
    .card-actions button.run:hover { background: #166534; }

    @media (max-width: 768px) {
      :host { padding: 16px 16px 32px; }
      .card { width: 100%; }
    }
  `;Ps([W()],wn.prototype,"_search",2);Ps([W()],wn.prototype,"_recent",2);wn=Ps([Ze("bashgpt-dashboard")],wn);async function Wi(t){try{const e=await t.json();if(e&&typeof e.error=="string"&&e.error.trim().length>0)return e.error}catch{}return`HTTP ${t.status}`}async function is(t){if(!t.ok)throw new Error(await Wi(t))}async function ma(t,e,n,i,s,a){const r=await fetch("/api/chat/stream",{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({prompt:t,...n?{sessionId:n}:{},...i?.length?{enabledTools:i}:{},...s?{agentId:s}:{},...a?{requestId:a}:{}})});if(!r.ok)throw new Error(await Wi(r));const d=r.body.getReader(),o=new TextDecoder;let p="",u=null;try{for(;;){const{done:g,value:m}=await d.read();if(g)break;p+=o.decode(m,{stream:!0});const y=p.split(`
`);p=y.pop()??"";for(const E of y){if(!E.startsWith("data: "))continue;const R=E.slice(6).trim();if(R==="[DONE]")continue;let B;try{B=JSON.parse(R)}catch{continue}const j=B?.choices?.[0]?.delta;if(j){if(j.bashgpt){const{event:P,data:D}=j.bashgpt;if(P==="tool_call")e.onToolCall?.(D);else if(P==="command_result")e.onCommandResult?.(D);else if(P==="round_start")e.onRoundStart?.(D);else if(P==="error"){const z=typeof D=="object"&&D!==null&&"message"in D&&typeof D.message=="string"?D.message:"Serverfehler";throw new Error(z)}}else j.reasoning?e.onReasoningToken?.(j.reasoning):j.content&&e.onToken?.(j.content);if(B?.bashgpt?.event==="done"){const P=B.bashgpt,D=typeof B.usage?.promptTokens=="number"&&typeof B.usage?.completionTokens=="number"?{inputTokens:B.usage.promptTokens,outputTokens:B.usage.completionTokens}:void 0;u={response:P.response,finalStatus:P.finalStatus,commands:P.commands??[],usage:D}}}}}}finally{d.releaseLock()}if(!u)throw new Error("Keine Antwort vom Server erhalten.");return u}async function _a(t){const e=await fetch("/api/chat/cancel",{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({requestId:t})});return await is(e),(await e.json())?.cancelled===!0}async function ji(){const t=await fetch("/api/history");await is(t);const e=await t.json();return Array.isArray(e.history)?e.history:[]}async function qi(){const t=await fetch("/api/reset",{method:"POST"});await is(t)}async function Ft(){try{const t=await fetch("/api/sessions");if(!t.ok)return null;const e=await t.json();return Array.isArray(e.sessions)?e.sessions:[]}catch{return null}}async function fi(){try{const t=await fetch("/api/sessions",{method:"POST"});return t.ok?t.json():null}catch{return null}}async function xa(t){try{const e=await fetch(`/api/sessions/${t}`);return e.ok?e.json():null}catch{return null}}async function ya(t,e){try{await fetch(`/api/sessions/${t}`,{method:"PUT",headers:{"Content-Type":"application/json"},body:JSON.stringify({...e,id:t})})}catch{}}async function wa(t){try{await fetch(`/api/sessions/${t}`,{method:"DELETE"})}catch{}}async function Ea(){try{await fetch("/api/sessions/clear",{method:"POST"})}catch{}}async function va(){try{const t=await fetch("/api/settings");return t.ok?t.json():null}catch{return null}}async function ka(t){const e=await fetch("/api/settings",{method:"PUT",headers:{"Content-Type":"application/json"},body:JSON.stringify(t)});await is(e)}async function Sa(){try{return await(await fetch("/api/settings/test",{method:"POST"})).json()}catch(t){return{ok:!1,error:t instanceof Error?t.message:String(t)}}}async function Ta(){try{const t=await fetch("/api/agents");if(!t.ok)return[];const e=await t.json();return Array.isArray(e.agents)?e.agents:[]}catch{return[]}}async function Aa(t){try{const e=await fetch(`/api/agents/${encodeURIComponent(t)}/info-panel`);if(!e.ok)return"";const n=await e.json();return typeof n.markdown=="string"?n.markdown:""}catch{return""}}async function Ki(){try{const t=await fetch("/api/tools");if(!t.ok)return[];const e=await t.json();return Array.isArray(e.tools)?e.tools:[]}catch{return[]}}var $a=Object.defineProperty,Ra=Object.getOwnPropertyDescriptor,Xt=(t,e,n,i)=>{for(var s=i>1?void 0:i?Ra(e,n):e,a=t.length-1,r;a>=0;a--)(r=t[a])&&(s=(i?r(e,n,s):r(s))||s);return i&&s&&$a(e,n,s),s};let ft=class extends Te{constructor(){super(...arguments),this._settings=null,this._loading=!1,this._testing=!1,this._status="",this._statusOk=!0}async connectedCallback(){super.connectedCallback(),this._loading=!0,this._settings=this._normalizeSettings(await va()),this._loading=!1,this._settings||(this._status="Einstellungen konnten nicht geladen werden. Bitte stelle sicher, dass der Server läuft.",this._statusOk=!1)}_normalizeSettings(t){if(!t)return null;const e="ollama",n=t.ollama?.model??t.model??"gpt-oss:20b",i=t.ollama?.host??t.ollamaHost??"http://localhost:11434";return{...t,provider:e,model:n,ollamaHost:i,ollama:{model:n,host:i}}}_setOllamaModel(t){if(!this._settings)return;const e={...this._settings,ollama:{...this._settings.ollama,model:t}};this._settings={...e,...e.provider==="ollama"?{model:t}:{}},this._status=""}_setOllamaHost(t){this._settings&&(this._settings={...this._settings,ollamaHost:t,ollama:{...this._settings.ollama,host:t}},this._status="")}_buildSavePayload(t){return{provider:t.provider,model:t.ollama.model,ollamaHost:t.ollama.host,ollama:{model:t.ollama.model,host:t.ollama.host}}}async _save(){if(this._settings){if(!this._settings.ollama.host.trim()){this._status="Ollama Host darf nicht leer sein.",this._statusOk=!1;return}this._loading=!0;try{await ka(this._buildSavePayload(this._settings)),this._status="Einstellungen gespeichert.",this._statusOk=!0}catch(t){this._status=`Fehler: ${t instanceof Error?t.message:String(t)}`,this._statusOk=!1}finally{this._loading=!1}}}async _test(){this._testing=!0,this._status="";const t=await Sa();this._testing=!1,t.ok?(this._status=`Verbindung OK${t.latencyMs!=null?` (${t.latencyMs} ms)`:""}`,this._statusOk=!0):(this._status=`Verbindung fehlgeschlagen: ${t.error??"Unbekannt"}`,this._statusOk=!1)}_clearHistory(){confirm("Gesamten Verlauf wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden.")&&this.dispatchEvent(new CustomEvent("clear-history",{bubbles:!0,composed:!0}))}_renderProviderDocumentation(){return T`
      <h3>Ollama Doku</h3>
      <p>Diese Optionen werden im Request an <code>/v1/chat/completions</code> gesendet (OpenAI-kompatibler Endpunkt). Kurz-Hinweise findest du direkt unter den Eingabefeldern links.</p>
      <div class="doc-group">
        <span class="doc-label">Basis</span>
        <ul class="doc-list">
          <li><code>model</code> - lokales Modell, z.B. <code>gpt-oss:20b</code>. Wirkung: steuert Fähigkeit, RAM-Bedarf und Latenz.</li>
          <li><code>host</code> - Standard: <code>http://localhost:11434</code>. Wirkung: Zielinstanz für alle Ollama-Requests.</li>
        </ul>
      </div>
      <div class="doc-group">
        <span class="doc-label">Sampling</span>
        <ul class="doc-list">
          <li>Weitere Sampling-Parameter werden im Open-Source-Build derzeit nicht persistent konfiguriert.</li>
        </ul>
      </div>
      <div class="doc-links">
        <a href="https://ollama.readthedocs.io/en/api/" target="_blank" rel="noreferrer">Ollama API Reference</a>
        <a href="https://ollama.com/blog/openai-compatibility" target="_blank" rel="noreferrer">Ollama OpenAI Compatibility</a>
      </div>
    `}render(){const t=this._settings;return this._loading&&!t?T`
        <h2>Einstellungen</h2>
        <div class="loading-placeholder">
          <div class="spinner"></div> Einstellungen werden geladen…
        </div>
      `:T`
      <h2>Einstellungen</h2>
      <div class="layout">
      <div class="settings-main">
      <div class="section">
        <div class="section-label">Verbindung</div>

        <div class="field">
          <label>Provider</label>
          <input type="text" .value=${t?.provider??"ollama"} disabled />
          <div class="hint">Open-Source-Builds nutzen ausschließlich Ollama.</div>
        </div>
        <div class="field">
          <label>Ollama Modell</label>
          <input
            type="text"
            .value=${t?.ollama.model??""}
            @input=${e=>this._setOllamaModel(e.target.value)}
            ?disabled=${!t||this._loading}
            placeholder="z.B. gpt-oss:20b"
          />
          <div class="hint">Steuert Qualität, Geschwindigkeit und Ressourcenbedarf.</div>
        </div>

        <div class="field">
          <label>Ollama Host</label>
          <input
            type="text"
            .value=${t?.ollama.host??"http://localhost:11434"}
            @input=${e=>this._setOllamaHost(e.target.value)}
            ?disabled=${!t||this._loading}
          />
          <div class="hint">Standard lokal: <code>http://localhost:11434</code>.</div>
        </div>

        <div class="actions">
          <button @click=${this._test} ?disabled=${!t||this._testing||this._loading}>
            ${this._testing?"Teste…":"Verbindung testen"}
          </button>
        </div>
      </div>

      <div class="section">
        <div class="section-label">Verlauf</div>
        <div class="hint">Löscht alle gespeicherten Sessions aus dem Browser-Speicher und setzt die Server-History zurück.</div>
        <div class="actions" style="margin-top: 12px;">
          <button class="danger" @click=${this._clearHistory}>
            Gesamten Verlauf löschen
          </button>
        </div>
      </div>

      <div class="divider"></div>

      <div class="actions">
        <button class="primary" @click=${this._save} ?disabled=${!t||this._loading}>
          ${this._loading?"Speichert…":"Speichern"}
        </button>
      </div>

      <div aria-live="polite" aria-atomic="true">
        ${this._status?T`
          <div class="status-msg ${this._statusOk?"ok":"error"}" role=${this._statusOk?"status":"alert"}>
            ${this._status}
          </div>
        `:""}
      </div>
      </div>
      <aside class="provider-doc" aria-label="Provider Dokumentation">
        ${this._renderProviderDocumentation()}
      </aside>
      </div>
    `}};ft.styles=Ke`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow-y: auto;
      padding: 28px 28px 40px;
      box-sizing: border-box;
      width: 100%;
    }

    h2 {
      font-size: 20px;
      font-weight: 700;
      color: #f1f5f9;
      margin: 0 0 24px;
    }
    .layout {
      display: grid;
      grid-template-columns: minmax(0, 1fr) 360px;
      gap: 24px;
      align-items: start;
    }
    .settings-main {
      min-width: 0;
    }
    .provider-doc {
      border: 1px solid #1e293b;
      background: #0b1220;
      border-radius: 12px;
      padding: 16px;
      position: sticky;
      top: 12px;
    }
    .provider-doc h3 {
      margin: 0 0 8px;
      font-size: 16px;
      color: #e2e8f0;
    }
    .provider-doc p {
      margin: 0 0 12px;
      color: #94a3b8;
      font-size: 13px;
      line-height: 1.45;
    }
    .doc-group {
      margin-bottom: 14px;
    }
    .doc-label {
      display: block;
      font-size: 11px;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: #475569;
      margin-bottom: 6px;
    }
    .doc-list {
      margin: 0;
      padding-left: 16px;
      color: #cbd5e1;
      font-size: 13px;
      line-height: 1.45;
    }
    .doc-list li + li {
      margin-top: 4px;
    }
    .doc-links {
      display: flex;
      flex-direction: column;
      gap: 6px;
      margin-top: 8px;
    }
    .doc-links a {
      color: #86efac;
      text-decoration: none;
      font-size: 12px;
    }
    .doc-links a:hover {
      text-decoration: underline;
    }

    .section {
      margin-bottom: 28px;
    }
    .section-label {
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.08em;
      color: #475569;
      text-transform: uppercase;
      margin-bottom: 12px;
      padding-bottom: 6px;
      border-bottom: 1px solid #1e293b;
    }

    .field {
      display: flex;
      flex-direction: column;
      gap: 5px;
      margin-bottom: 14px;
    }
    label {
      font-size: 13px;
      color: #94a3b8;
    }
    input, select {
      background: #111827;
      color: #e5e7eb;
      border: 1px solid #374151;
      border-radius: 8px;
      padding: 9px 12px;
      font-size: 14px;
      outline: none;
      transition: border-color 0.15s;
      font-family: inherit;
    }
    input:focus, select:focus { border-color: #4b5563; }
    input:focus-visible, select:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }
    .input-with-action {
      display: flex;
      align-items: stretch;
      gap: 8px;
    }
    .input-with-action input {
      flex: 1;
      min-width: 0;
    }
    .icon-btn {
      width: 40px;
      padding: 0;
      border-radius: 8px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      font-size: 16px;
      line-height: 1;
    }

    .toggle-row {
      display: flex;
      align-items: center;
      gap: 10px;
    }
    input[type=checkbox] {
      width: 16px;
      height: 16px;
      accent-color: #22c55e;
      cursor: pointer;
      padding: 0;
    }

    .actions {
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
      margin-top: 8px;
    }

    button {
      padding: 8px 16px;
      border-radius: 8px;
      font-size: 13px;
      cursor: pointer;
      border: 1px solid #374151;
      background: #1e293b;
      color: #e5e7eb;
      transition: background 0.12s;
    }
    button:hover:not(:disabled) { background: #334155; }
    button:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }
    button:disabled { opacity: 0.4; cursor: not-allowed; }

    button.primary {
      background: #14532d;
      border-color: #16a34a;
      color: #dcfce7;
      font-weight: 600;
    }
    button.primary:hover:not(:disabled) { background: #166534; }

    button.danger {
      background: #7f1d1d;
      border-color: #991b1b;
      color: #fca5a5;
    }
    button.danger:hover:not(:disabled) { background: #991b1b; }

    .status-msg {
      margin-top: 14px;
      font-size: 13px;
      padding: 8px 12px;
      border-radius: 8px;
    }
    .status-msg.ok    { background: #14532d; color: #86efac; }
    .status-msg.error { background: #7f1d1d; color: #fca5a5; }

    .divider {
      height: 1px;
      background: #1e293b;
      margin: 4px 0 20px;
    }

    .hint {
      font-size: 12px;
      color: #475569;
      margin-top: 2px;
    }

    .loading-placeholder {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 40px 0;
      color: #475569;
      font-size: 14px;
    }
    .loading-placeholder .spinner {
      width: 16px; height: 16px;
      border: 2px solid #1e293b;
      border-top-color: #22c55e;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
      flex-shrink: 0;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    @media (max-width: 768px) {
      :host { padding: 16px 16px 32px; }
      .layout {
        grid-template-columns: 1fr;
        gap: 16px;
      }
      .provider-doc {
        position: static;
      }
    }
  `;Xt([W()],ft.prototype,"_settings",2);Xt([W()],ft.prototype,"_loading",2);Xt([W()],ft.prototype,"_testing",2);Xt([W()],ft.prototype,"_status",2);Xt([W()],ft.prototype,"_statusOk",2);ft=Xt([Ze("bashgpt-settings-view")],ft);var Oa=Object.defineProperty,Ca=Object.getOwnPropertyDescriptor,rs=(t,e,n,i)=>{for(var s=i>1?void 0:i?Ca(e,n):e,a=t.length-1,r;a>=0;a--)(r=t[a])&&(s=(i?r(e,n,s):r(s))||s);return i&&s&&Oa(e,n,s),s};let qt=class extends Te{constructor(){super(...arguments),this._agents=[],this._loading=!0,this._error=""}async connectedCallback(){super.connectedCallback(),await this._load()}async _load(){this._loading=!0,this._error="";try{this._agents=await Ta()}catch(t){this._error=t instanceof Error?t.message:String(t)}finally{this._loading=!1}}_startChat(t){this.dispatchEvent(new CustomEvent("agent-chat-start",{detail:{agentId:t.id},bubbles:!0,composed:!0}))}render(){return T`
      <h2>Agents</h2>
      <div class="subtitle">Predefined AI assistants. Pick one to start a focused chat.</div>

      ${this._error?T`<div class="error-msg">${this._error}</div>`:""}

      ${this._loading?T`<div class="empty">Loading agents...</div>`:this._agents.length===0?T`<div class="empty">No agents available.</div>`:T`
            <div class="agent-list">
              ${vn(this._agents,t=>t.id,t=>this._renderAgent(t))}
            </div>
          `}
    `}_renderAgent(t){return T`
      <div class="agent-card">
        <div class="agent-icon">AI</div>
        <div class="agent-body">
          <div class="agent-name">${t.name}</div>
          <div class="agent-meta">${t.id}</div>
        </div>
        <div class="agent-actions">
          <button class="btn-chat" @click=${()=>this._startChat(t)}>Start chat</button>
        </div>
      </div>
    `}};qt.styles=Ke`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      padding: 24px;
      overflow-y: auto;
      box-sizing: border-box;
    }

    h2 {
      margin: 0 0 4px;
      font-size: 18px;
      font-weight: 700;
      color: #f1f5f9;
    }

    .subtitle {
      font-size: 13px;
      color: #475569;
      margin-bottom: 20px;
    }

    .empty {
      text-align: center;
      color: #475569;
      font-size: 14px;
      padding: 48px 0;
    }

    .agent-list {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    .agent-card {
      background: #0f172a;
      border: 1px solid #1e293b;
      border-radius: 10px;
      padding: 14px 16px;
      display: flex;
      align-items: flex-start;
      gap: 12px;
    }

    .agent-icon {
      font-size: 20px;
      line-height: 1;
      margin-top: 2px;
    }

    .agent-body { flex: 1; min-width: 0; }

    .agent-name {
      font-size: 14px;
      font-weight: 600;
      color: #f1f5f9;
      margin-bottom: 4px;
    }

    .agent-meta {
      font-size: 12px;
      color: #475569;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .agent-actions {
      display: flex;
      gap: 6px;
      flex-shrink: 0;
      align-items: flex-start;
    }

    .btn-chat {
      background: #14532d;
      border: 1px solid #16a34a;
      color: #86efac;
      font-size: 12px;
      padding: 5px 10px;
      border-radius: 6px;
      cursor: pointer;
      transition: background 0.12s;
    }
    .btn-chat:hover { background: #166534; }

    .error-msg {
      color: #fca5a5;
      font-size: 13px;
      padding: 12px;
      background: #1e0a0a;
      border-radius: 8px;
      border: 1px solid #7f1d1d;
    }
  `;rs([W()],qt.prototype,"_agents",2);rs([W()],qt.prototype,"_loading",2);rs([W()],qt.prototype,"_error",2);qt=rs([Ze("bashgpt-agents-view")],qt);var Na=Object.defineProperty,Ia=Object.getOwnPropertyDescriptor,as=(t,e,n,i)=>{for(var s=i>1?void 0:i?Ia(e,n):e,a=t.length-1,r;a>=0;a--)(r=t[a])&&(s=(i?r(e,n,s):r(s))||s);return i&&s&&Na(e,n,s),s};let Kt=class extends Te{constructor(){super(...arguments),this._tools=[],this._loading=!0,this._error=""}async connectedCallback(){super.connectedCallback(),await this._load()}async _load(){this._loading=!0,this._error="";try{this._tools=await Ki()}catch(t){this._error=t instanceof Error?t.message:String(t)}finally{this._loading=!1}}render(){return T`
      <h2>Tools</h2>
      <div class="subtitle">Registered tools available in chat sessions and agents.</div>

      ${this._error?T`<div class="error">${this._error}</div>`:""}

      ${this._loading?T`<div class="loading">Loading tools...</div>`:this._tools.length===0?T`<div class="empty">No tools registered.</div>`:T`
            <div class="tool-list">
              ${vn(this._tools,t=>t.name,t=>T`
                <div class="tool-card">
                  <div class="tool-header">
                    <span class="tool-name">${t.name}</span>
                  </div>
                  ${t.description?T`<div class="tool-desc">${t.description}</div>`:""}
                  ${t.parameters?.length?T`
                    <div class="params-label">Parameters</div>
                    ${t.parameters.map(e=>T`
                      <div class="param-row">
                        <span class="param-name">${e.name}</span>
                        <span class="param-type">${e.type}</span>
                        <span class="param-desc">${e.description}</span>
                        <span class="${e.required?"param-required":"param-optional"}">
                          ${e.required?"required":"optional"}
                        </span>
                      </div>
                    `)}
                  `:""}
                </div>
              `)}
            </div>
          `}
    `}};Kt.styles=Ke`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      padding: 24px;
      overflow-y: auto;
      box-sizing: border-box;
    }

    h2 {
      margin: 0 0 4px;
      font-size: 18px;
      font-weight: 700;
      color: #f1f5f9;
    }

    .subtitle {
      font-size: 13px;
      color: #64748b;
      margin-bottom: 24px;
    }

    .error {
      color: #ef4444;
      font-size: 13px;
      padding: 12px;
      background: #1c0a0a;
      border: 1px solid #7f1d1d;
      border-radius: 8px;
      margin-bottom: 16px;
    }

    .loading {
      color: #475569;
      font-size: 13px;
    }

    .empty {
      color: #475569;
      font-size: 13px;
      padding: 32px 0;
      text-align: center;
    }

    .tool-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .tool-card {
      background: #0f172a;
      border: 1px solid #1e293b;
      border-radius: 10px;
      padding: 16px;
    }

    .tool-header {
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 6px;
    }

    .tool-name {
      font-size: 14px;
      font-weight: 600;
      color: #f1f5f9;
      font-family: monospace;
    }

    .tool-desc {
      font-size: 13px;
      color: #94a3b8;
      margin-bottom: 10px;
      line-height: 1.5;
    }

    .params-label {
      font-size: 11px;
      font-weight: 600;
      color: #475569;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      margin-bottom: 6px;
    }

    .param-row {
      display: flex;
      align-items: baseline;
      gap: 8px;
      padding: 5px 0;
      border-top: 1px solid #1e293b;
      font-size: 12px;
    }

    .param-name {
      font-family: monospace;
      color: #7dd3fc;
      min-width: 100px;
    }

    .param-type {
      color: #a78bfa;
      min-width: 60px;
    }

    .param-desc {
      color: #64748b;
      flex: 1;
    }

    .param-required {
      font-size: 10px;
      font-weight: 600;
      padding: 1px 6px;
      border-radius: 999px;
      background: #14532d;
      color: #86efac;
      flex-shrink: 0;
    }

    .param-optional {
      font-size: 10px;
      font-weight: 600;
      padding: 1px 6px;
      border-radius: 999px;
      background: #1e293b;
      color: #64748b;
      flex-shrink: 0;
    }
  `;as([W()],Kt.prototype,"_tools",2);as([W()],Kt.prototype,"_loading",2);as([W()],Kt.prototype,"_error",2);Kt=as([Ze("bashgpt-tools-view")],Kt);class Ss extends Gi{constructor(e){if(super(e),this.it=oe,e.type!==Hi.CHILD)throw Error(this.constructor.directiveName+"() can only be used in child bindings")}render(e){if(e===oe||e==null)return this._t=void 0,this.it=e;if(e===gt)return e;if(typeof e!="string")throw Error(this.constructor.directiveName+"() called with a non-string value");if(e===this.it)return this._t;this.it=e;const n=[e];return n.raw=n,this._t={_$litType$:this.constructor.resultType,strings:n,values:[]}}}Ss.directiveName="unsafeHTML",Ss.resultType=1;const Zi=Fi(Ss);const{entries:Vi,setPrototypeOf:bi,isFrozen:Ma,getPrototypeOf:La,getOwnPropertyDescriptor:Pa}=Object;let{freeze:ve,seal:Me,create:Kn}=Object,{apply:Ts,construct:As}=typeof Reflect<"u"&&Reflect;ve||(ve=function(e){return e});Me||(Me=function(e){return e});Ts||(Ts=function(e,n){for(var i=arguments.length,s=new Array(i>2?i-2:0),a=2;a<i;a++)s[a-2]=arguments[a];return e.apply(n,s)});As||(As=function(e){for(var n=arguments.length,i=new Array(n>1?n-1:0),s=1;s<n;s++)i[s-1]=arguments[s];return new e(...i)});const Hn=ke(Array.prototype.forEach),Da=ke(Array.prototype.lastIndexOf),mi=ke(Array.prototype.pop),an=ke(Array.prototype.push),za=ke(Array.prototype.splice),Zn=ke(String.prototype.toLowerCase),_s=ke(String.prototype.toString),xs=ke(String.prototype.match),on=ke(String.prototype.replace),Ua=ke(String.prototype.indexOf),Ba=ke(String.prototype.trim),Ce=ke(Object.prototype.hasOwnProperty),ye=ke(RegExp.prototype.test),ln=Ha(TypeError);function ke(t){return function(e){e instanceof RegExp&&(e.lastIndex=0);for(var n=arguments.length,i=new Array(n>1?n-1:0),s=1;s<n;s++)i[s-1]=arguments[s];return Ts(t,e,i)}}function Ha(t){return function(){for(var e=arguments.length,n=new Array(e),i=0;i<e;i++)n[i]=arguments[i];return As(t,n)}}function L(t,e){let n=arguments.length>2&&arguments[2]!==void 0?arguments[2]:Zn;bi&&bi(t,null);let i=e.length;for(;i--;){let s=e[i];if(typeof s=="string"){const a=n(s);a!==s&&(Ma(e)||(e[i]=a),s=a)}t[s]=!0}return t}function Fa(t){for(let e=0;e<t.length;e++)Ce(t,e)||(t[e]=null);return t}function je(t){const e=Kn(null);for(const[n,i]of Vi(t))Ce(t,n)&&(Array.isArray(i)?e[n]=Fa(i):i&&typeof i=="object"&&i.constructor===Object?e[n]=je(i):e[n]=i);return e}function cn(t,e){for(;t!==null;){const i=Pa(t,e);if(i){if(i.get)return ke(i.get);if(typeof i.value=="function")return ke(i.value)}t=La(t)}function n(){return null}return n}const _i=ve(["a","abbr","acronym","address","area","article","aside","audio","b","bdi","bdo","big","blink","blockquote","body","br","button","canvas","caption","center","cite","code","col","colgroup","content","data","datalist","dd","decorator","del","details","dfn","dialog","dir","div","dl","dt","element","em","fieldset","figcaption","figure","font","footer","form","h1","h2","h3","h4","h5","h6","head","header","hgroup","hr","html","i","img","input","ins","kbd","label","legend","li","main","map","mark","marquee","menu","menuitem","meter","nav","nobr","ol","optgroup","option","output","p","picture","pre","progress","q","rp","rt","ruby","s","samp","search","section","select","shadow","slot","small","source","spacer","span","strike","strong","style","sub","summary","sup","table","tbody","td","template","textarea","tfoot","th","thead","time","tr","track","tt","u","ul","var","video","wbr"]),ys=ve(["svg","a","altglyph","altglyphdef","altglyphitem","animatecolor","animatemotion","animatetransform","circle","clippath","defs","desc","ellipse","enterkeyhint","exportparts","filter","font","g","glyph","glyphref","hkern","image","inputmode","line","lineargradient","marker","mask","metadata","mpath","part","path","pattern","polygon","polyline","radialgradient","rect","stop","style","switch","symbol","text","textpath","title","tref","tspan","view","vkern"]),ws=ve(["feBlend","feColorMatrix","feComponentTransfer","feComposite","feConvolveMatrix","feDiffuseLighting","feDisplacementMap","feDistantLight","feDropShadow","feFlood","feFuncA","feFuncB","feFuncG","feFuncR","feGaussianBlur","feImage","feMerge","feMergeNode","feMorphology","feOffset","fePointLight","feSpecularLighting","feSpotLight","feTile","feTurbulence"]),Ga=ve(["animate","color-profile","cursor","discard","font-face","font-face-format","font-face-name","font-face-src","font-face-uri","foreignobject","hatch","hatchpath","mesh","meshgradient","meshpatch","meshrow","missing-glyph","script","set","solidcolor","unknown","use"]),Es=ve(["math","menclose","merror","mfenced","mfrac","mglyph","mi","mlabeledtr","mmultiscripts","mn","mo","mover","mpadded","mphantom","mroot","mrow","ms","mspace","msqrt","mstyle","msub","msup","msubsup","mtable","mtd","mtext","mtr","munder","munderover","mprescripts"]),Wa=ve(["maction","maligngroup","malignmark","mlongdiv","mscarries","mscarry","msgroup","mstack","msline","msrow","semantics","annotation","annotation-xml","mprescripts","none"]),xi=ve(["#text"]),yi=ve(["accept","action","align","alt","autocapitalize","autocomplete","autopictureinpicture","autoplay","background","bgcolor","border","capture","cellpadding","cellspacing","checked","cite","class","clear","color","cols","colspan","controls","controlslist","coords","crossorigin","datetime","decoding","default","dir","disabled","disablepictureinpicture","disableremoteplayback","download","draggable","enctype","enterkeyhint","exportparts","face","for","headers","height","hidden","high","href","hreflang","id","inert","inputmode","integrity","ismap","kind","label","lang","list","loading","loop","low","max","maxlength","media","method","min","minlength","multiple","muted","name","nonce","noshade","novalidate","nowrap","open","optimum","part","pattern","placeholder","playsinline","popover","popovertarget","popovertargetaction","poster","preload","pubdate","radiogroup","readonly","rel","required","rev","reversed","role","rows","rowspan","spellcheck","scope","selected","shape","size","sizes","slot","span","srclang","start","src","srcset","step","style","summary","tabindex","title","translate","type","usemap","valign","value","width","wrap","xmlns","slot"]),vs=ve(["accent-height","accumulate","additive","alignment-baseline","amplitude","ascent","attributename","attributetype","azimuth","basefrequency","baseline-shift","begin","bias","by","class","clip","clippathunits","clip-path","clip-rule","color","color-interpolation","color-interpolation-filters","color-profile","color-rendering","cx","cy","d","dx","dy","diffuseconstant","direction","display","divisor","dur","edgemode","elevation","end","exponent","fill","fill-opacity","fill-rule","filter","filterunits","flood-color","flood-opacity","font-family","font-size","font-size-adjust","font-stretch","font-style","font-variant","font-weight","fx","fy","g1","g2","glyph-name","glyphref","gradientunits","gradienttransform","height","href","id","image-rendering","in","in2","intercept","k","k1","k2","k3","k4","kerning","keypoints","keysplines","keytimes","lang","lengthadjust","letter-spacing","kernelmatrix","kernelunitlength","lighting-color","local","marker-end","marker-mid","marker-start","markerheight","markerunits","markerwidth","maskcontentunits","maskunits","max","mask","mask-type","media","method","mode","min","name","numoctaves","offset","operator","opacity","order","orient","orientation","origin","overflow","paint-order","path","pathlength","patterncontentunits","patterntransform","patternunits","points","preservealpha","preserveaspectratio","primitiveunits","r","rx","ry","radius","refx","refy","repeatcount","repeatdur","restart","result","rotate","scale","seed","shape-rendering","slope","specularconstant","specularexponent","spreadmethod","startoffset","stddeviation","stitchtiles","stop-color","stop-opacity","stroke-dasharray","stroke-dashoffset","stroke-linecap","stroke-linejoin","stroke-miterlimit","stroke-opacity","stroke","stroke-width","style","surfacescale","systemlanguage","tabindex","tablevalues","targetx","targety","transform","transform-origin","text-anchor","text-decoration","text-rendering","textlength","type","u1","u2","unicode","values","viewbox","visibility","version","vert-adv-y","vert-origin-x","vert-origin-y","width","word-spacing","wrap","writing-mode","xchannelselector","ychannelselector","x","x1","x2","xmlns","y","y1","y2","z","zoomandpan"]),wi=ve(["accent","accentunder","align","bevelled","close","columnsalign","columnlines","columnspan","denomalign","depth","dir","display","displaystyle","encoding","fence","frame","height","href","id","largeop","length","linethickness","lspace","lquote","mathbackground","mathcolor","mathsize","mathvariant","maxsize","minsize","movablelimits","notation","numalign","open","rowalign","rowlines","rowspacing","rowspan","rspace","rquote","scriptlevel","scriptminsize","scriptsizemultiplier","selection","separator","separators","stretchy","subscriptshift","supscriptshift","symmetric","voffset","width","xmlns"]),Fn=ve(["xlink:href","xml:id","xlink:title","xml:space","xmlns:xlink"]),ja=Me(/\{\{[\w\W]*|[\w\W]*\}\}/gm),qa=Me(/<%[\w\W]*|[\w\W]*%>/gm),Ka=Me(/\$\{[\w\W]*/gm),Za=Me(/^data-[\-\w.\u00B7-\uFFFF]+$/),Va=Me(/^aria-[\-\w]+$/),Xi=Me(/^(?:(?:(?:f|ht)tps?|mailto|tel|callto|sms|cid|xmpp|matrix):|[^a-z]|[a-z+.\-]+(?:[^a-z+.\-:]|$))/i),Xa=Me(/^(?:\w+script|data):/i),Ya=Me(/[\u0000-\u0020\u00A0\u1680\u180E\u2000-\u2029\u205F\u3000]/g),Yi=Me(/^html$/i),Qa=Me(/^[a-z][.\w]*(-[.\w]+)+$/i);var Ei=Object.freeze({__proto__:null,ARIA_ATTR:Va,ATTR_WHITESPACE:Ya,CUSTOM_ELEMENT:Qa,DATA_ATTR:Za,DOCTYPE_NAME:Yi,ERB_EXPR:qa,IS_ALLOWED_URI:Xi,IS_SCRIPT_OR_DATA:Xa,MUSTACHE_EXPR:ja,TMPLIT_EXPR:Ka});const dn={element:1,text:3,progressingInstruction:7,comment:8,document:9},Ja=function(){return typeof window>"u"?null:window},eo=function(e,n){if(typeof e!="object"||typeof e.createPolicy!="function")return null;let i=null;const s="data-tt-policy-suffix";n&&n.hasAttribute(s)&&(i=n.getAttribute(s));const a="dompurify"+(i?"#"+i:"");try{return e.createPolicy(a,{createHTML(r){return r},createScriptURL(r){return r}})}catch{return console.warn("TrustedTypes policy "+a+" could not be created."),null}},vi=function(){return{afterSanitizeAttributes:[],afterSanitizeElements:[],afterSanitizeShadowDOM:[],beforeSanitizeAttributes:[],beforeSanitizeElements:[],beforeSanitizeShadowDOM:[],uponSanitizeAttribute:[],uponSanitizeElement:[],uponSanitizeShadowNode:[]}};function Qi(){let t=arguments.length>0&&arguments[0]!==void 0?arguments[0]:Ja();const e=A=>Qi(A);if(e.version="3.3.2",e.removed=[],!t||!t.document||t.document.nodeType!==dn.document||!t.Element)return e.isSupported=!1,e;let{document:n}=t;const i=n,s=i.currentScript,{DocumentFragment:a,HTMLTemplateElement:r,Node:d,Element:o,NodeFilter:p,NamedNodeMap:u=t.NamedNodeMap||t.MozNamedAttrMap,HTMLFormElement:g,DOMParser:m,trustedTypes:y}=t,E=o.prototype,R=cn(E,"cloneNode"),B=cn(E,"remove"),j=cn(E,"nextSibling"),P=cn(E,"childNodes"),D=cn(E,"parentNode");if(typeof r=="function"){const A=n.createElement("template");A.content&&A.content.ownerDocument&&(n=A.content.ownerDocument)}let z,Y="";const{implementation:te,createNodeIterator:tt,createDocumentFragment:Le,getElementsByTagName:nt}=n,{importNode:st}=i;let ie=vi();e.isSupported=typeof Vi=="function"&&typeof D=="function"&&te&&te.createHTMLDocument!==void 0;const{MUSTACHE_EXPR:Ve,ERB_EXPR:Xe,TMPLIT_EXPR:Ae,DATA_ATTR:bt,ARIA_ATTR:Ye,IS_SCRIPT_OR_DATA:mt,ATTR_WHITESPACE:H,CUSTOM_ELEMENT:he}=Ei;let{IS_ALLOWED_URI:be}=Ei,q=null;const Ne=L({},[..._i,...ys,...ws,...Es,...xi]);let J=null;const Sn=L({},[...yi,...vs,...wi,...Fn]);let ne=Object.seal(Kn(null,{tagNameCheck:{writable:!0,configurable:!1,enumerable:!0,value:null},attributeNameCheck:{writable:!0,configurable:!1,enumerable:!0,value:null},allowCustomizedBuiltInElements:{writable:!0,configurable:!1,enumerable:!0,value:!1}})),_t=null,It=null;const Fe=Object.seal(Kn(null,{tagCheck:{writable:!0,configurable:!1,enumerable:!0,value:null},attributeCheck:{writable:!0,configurable:!1,enumerable:!0,value:null}}));let ps=!0,it=!0,Tn=!1,An=!0,rt=!1,Mt=!0,Qe=!1,Yt=!1,Qt=!1,at=!1,Lt=!1,xt=!1,$n=!0,Rn=!1;const On="user-content-";let Pe=!0,ot=!1,$e={},xe=null;const Pt=L({},["annotation-xml","audio","colgroup","desc","foreignobject","head","iframe","math","mi","mn","mo","ms","mtext","noembed","noframes","noscript","plaintext","script","style","svg","template","thead","title","video","xmp"]);let Cn=null;const Nn=L({},["audio","video","img","source","image","track"]);let Jt=null;const In=L({},["alt","class","for","id","label","name","pattern","placeholder","role","summary","title","value","style","xmlns"]),Dt="http://www.w3.org/1998/Math/MathML",yt="http://www.w3.org/2000/svg",De="http://www.w3.org/1999/xhtml";let lt=De,en=!1,wt=null;const Mn=L({},[Dt,yt,De],_s);let Et=L({},["mi","mo","mn","ms","mtext"]),zt=L({},["annotation-xml"]);const Ln=L({},["title","style","font","a","script"]);let Ie=null;const l=["application/xhtml+xml","text/html"],h="text/html";let b=null,O=null;const se=n.createElement("form"),ee=function(c){return c instanceof RegExp||c instanceof Function},k=function(){let c=arguments.length>0&&arguments[0]!==void 0?arguments[0]:{};if(!(O&&O===c)){if((!c||typeof c!="object")&&(c={}),c=je(c),Ie=l.indexOf(c.PARSER_MEDIA_TYPE)===-1?h:c.PARSER_MEDIA_TYPE,b=Ie==="application/xhtml+xml"?_s:Zn,q=Ce(c,"ALLOWED_TAGS")?L({},c.ALLOWED_TAGS,b):Ne,J=Ce(c,"ALLOWED_ATTR")?L({},c.ALLOWED_ATTR,b):Sn,wt=Ce(c,"ALLOWED_NAMESPACES")?L({},c.ALLOWED_NAMESPACES,_s):Mn,Jt=Ce(c,"ADD_URI_SAFE_ATTR")?L(je(In),c.ADD_URI_SAFE_ATTR,b):In,Cn=Ce(c,"ADD_DATA_URI_TAGS")?L(je(Nn),c.ADD_DATA_URI_TAGS,b):Nn,xe=Ce(c,"FORBID_CONTENTS")?L({},c.FORBID_CONTENTS,b):Pt,_t=Ce(c,"FORBID_TAGS")?L({},c.FORBID_TAGS,b):je({}),It=Ce(c,"FORBID_ATTR")?L({},c.FORBID_ATTR,b):je({}),$e=Ce(c,"USE_PROFILES")?c.USE_PROFILES:!1,ps=c.ALLOW_ARIA_ATTR!==!1,it=c.ALLOW_DATA_ATTR!==!1,Tn=c.ALLOW_UNKNOWN_PROTOCOLS||!1,An=c.ALLOW_SELF_CLOSE_IN_ATTR!==!1,rt=c.SAFE_FOR_TEMPLATES||!1,Mt=c.SAFE_FOR_XML!==!1,Qe=c.WHOLE_DOCUMENT||!1,at=c.RETURN_DOM||!1,Lt=c.RETURN_DOM_FRAGMENT||!1,xt=c.RETURN_TRUSTED_TYPE||!1,Qt=c.FORCE_BODY||!1,$n=c.SANITIZE_DOM!==!1,Rn=c.SANITIZE_NAMED_PROPS||!1,Pe=c.KEEP_CONTENT!==!1,ot=c.IN_PLACE||!1,be=c.ALLOWED_URI_REGEXP||Xi,lt=c.NAMESPACE||De,Et=c.MATHML_TEXT_INTEGRATION_POINTS||Et,zt=c.HTML_INTEGRATION_POINTS||zt,ne=c.CUSTOM_ELEMENT_HANDLING||{},c.CUSTOM_ELEMENT_HANDLING&&ee(c.CUSTOM_ELEMENT_HANDLING.tagNameCheck)&&(ne.tagNameCheck=c.CUSTOM_ELEMENT_HANDLING.tagNameCheck),c.CUSTOM_ELEMENT_HANDLING&&ee(c.CUSTOM_ELEMENT_HANDLING.attributeNameCheck)&&(ne.attributeNameCheck=c.CUSTOM_ELEMENT_HANDLING.attributeNameCheck),c.CUSTOM_ELEMENT_HANDLING&&typeof c.CUSTOM_ELEMENT_HANDLING.allowCustomizedBuiltInElements=="boolean"&&(ne.allowCustomizedBuiltInElements=c.CUSTOM_ELEMENT_HANDLING.allowCustomizedBuiltInElements),rt&&(it=!1),Lt&&(at=!0),$e&&(q=L({},xi),J=Kn(null),$e.html===!0&&(L(q,_i),L(J,yi)),$e.svg===!0&&(L(q,ys),L(J,vs),L(J,Fn)),$e.svgFilters===!0&&(L(q,ws),L(J,vs),L(J,Fn)),$e.mathMl===!0&&(L(q,Es),L(J,wi),L(J,Fn))),Ce(c,"ADD_TAGS")||(Fe.tagCheck=null),Ce(c,"ADD_ATTR")||(Fe.attributeCheck=null),c.ADD_TAGS&&(typeof c.ADD_TAGS=="function"?Fe.tagCheck=c.ADD_TAGS:(q===Ne&&(q=je(q)),L(q,c.ADD_TAGS,b))),c.ADD_ATTR&&(typeof c.ADD_ATTR=="function"?Fe.attributeCheck=c.ADD_ATTR:(J===Sn&&(J=je(J)),L(J,c.ADD_ATTR,b))),c.ADD_URI_SAFE_ATTR&&L(Jt,c.ADD_URI_SAFE_ATTR,b),c.FORBID_CONTENTS&&(xe===Pt&&(xe=je(xe)),L(xe,c.FORBID_CONTENTS,b)),c.ADD_FORBID_CONTENTS&&(xe===Pt&&(xe=je(xe)),L(xe,c.ADD_FORBID_CONTENTS,b)),Pe&&(q["#text"]=!0),Qe&&L(q,["html","head","body"]),q.table&&(L(q,["tbody"]),delete _t.tbody),c.TRUSTED_TYPES_POLICY){if(typeof c.TRUSTED_TYPES_POLICY.createHTML!="function")throw ln('TRUSTED_TYPES_POLICY configuration option must provide a "createHTML" hook.');if(typeof c.TRUSTED_TYPES_POLICY.createScriptURL!="function")throw ln('TRUSTED_TYPES_POLICY configuration option must provide a "createScriptURL" hook.');z=c.TRUSTED_TYPES_POLICY,Y=z.createHTML("")}else z===void 0&&(z=eo(y,s)),z!==null&&typeof Y=="string"&&(Y=z.createHTML(""));ve&&ve(c),O=c}},v=L({},[...ys,...ws,...Ga]),C=L({},[...Es,...Wa]),ae=function(c){let f=D(c);(!f||!f.tagName)&&(f={namespaceURI:lt,tagName:"template"});const S=Zn(c.tagName),X=Zn(f.tagName);return wt[c.namespaceURI]?c.namespaceURI===yt?f.namespaceURI===De?S==="svg":f.namespaceURI===Dt?S==="svg"&&(X==="annotation-xml"||Et[X]):!!v[S]:c.namespaceURI===Dt?f.namespaceURI===De?S==="math":f.namespaceURI===yt?S==="math"&&zt[X]:!!C[S]:c.namespaceURI===De?f.namespaceURI===yt&&!zt[X]||f.namespaceURI===Dt&&!Et[X]?!1:!C[S]&&(Ln[S]||!v[S]):!!(Ie==="application/xhtml+xml"&&wt[c.namespaceURI]):!1},G=function(c){an(e.removed,{element:c});try{D(c).removeChild(c)}catch{B(c)}},Re=function(c,f){try{an(e.removed,{attribute:f.getAttributeNode(c),from:f})}catch{an(e.removed,{attribute:null,from:f})}if(f.removeAttribute(c),c==="is")if(at||Lt)try{G(f)}catch{}else try{f.setAttribute(c,"")}catch{}},Ut=function(c){let f=null,S=null;if(Qt)c="<remove></remove>"+c;else{const re=xs(c,/^[\r\n\t ]+/);S=re&&re[0]}Ie==="application/xhtml+xml"&&lt===De&&(c='<html xmlns="http://www.w3.org/1999/xhtml"><head></head><body>'+c+"</body></html>");const X=z?z.createHTML(c):c;if(lt===De)try{f=new m().parseFromString(X,Ie)}catch{}if(!f||!f.documentElement){f=te.createDocument(lt,"template",null);try{f.documentElement.innerHTML=en?Y:X}catch{}}const pe=f.body||f.documentElement;return c&&S&&pe.insertBefore(n.createTextNode(S),pe.childNodes[0]||null),lt===De?nt.call(f,Qe?"html":"body")[0]:Qe?f.documentElement:pe},Bt=function(c){return tt.call(c.ownerDocument||c,c,p.SHOW_ELEMENT|p.SHOW_COMMENT|p.SHOW_TEXT|p.SHOW_PROCESSING_INSTRUCTION|p.SHOW_CDATA_SECTION,null)},tn=function(c){return c instanceof g&&(typeof c.nodeName!="string"||typeof c.textContent!="string"||typeof c.removeChild!="function"||!(c.attributes instanceof u)||typeof c.removeAttribute!="function"||typeof c.setAttribute!="function"||typeof c.namespaceURI!="string"||typeof c.insertBefore!="function"||typeof c.hasChildNodes!="function")},Ht=function(c){return typeof d=="function"&&c instanceof d};function ze(A,c,f){Hn(A,S=>{S.call(e,c,f,O)})}const Pn=function(c){let f=null;if(ze(ie.beforeSanitizeElements,c,null),tn(c))return G(c),!0;const S=b(c.nodeName);if(ze(ie.uponSanitizeElement,c,{tagName:S,allowedTags:q}),Mt&&c.hasChildNodes()&&!Ht(c.firstElementChild)&&ye(/<[/\w!]/g,c.innerHTML)&&ye(/<[/\w!]/g,c.textContent)||c.nodeType===dn.progressingInstruction||Mt&&c.nodeType===dn.comment&&ye(/<[/\w]/g,c.data))return G(c),!0;if(!(Fe.tagCheck instanceof Function&&Fe.tagCheck(S))&&(!q[S]||_t[S])){if(!_t[S]&&nn(S)&&(ne.tagNameCheck instanceof RegExp&&ye(ne.tagNameCheck,S)||ne.tagNameCheck instanceof Function&&ne.tagNameCheck(S)))return!1;if(Pe&&!xe[S]){const X=D(c)||c.parentNode,pe=P(c)||c.childNodes;if(pe&&X){const re=pe.length;for(let me=re-1;me>=0;--me){const Se=R(pe[me],!0);Se.__removalCount=(c.__removalCount||0)+1,X.insertBefore(Se,j(c))}}}return G(c),!0}return c instanceof o&&!ae(c)||(S==="noscript"||S==="noembed"||S==="noframes")&&ye(/<\/no(script|embed|frames)/i,c.innerHTML)?(G(c),!0):(rt&&c.nodeType===dn.text&&(f=c.textContent,Hn([Ve,Xe,Ae],X=>{f=on(f,X," ")}),c.textContent!==f&&(an(e.removed,{element:c.cloneNode()}),c.textContent=f)),ze(ie.afterSanitizeElements,c,null),!1)},Dn=function(c,f,S){if(It[f]||$n&&(f==="id"||f==="name")&&(S in n||S in se))return!1;if(!(it&&!It[f]&&ye(bt,f))){if(!(ps&&ye(Ye,f))){if(!(Fe.attributeCheck instanceof Function&&Fe.attributeCheck(f,c))){if(!J[f]||It[f]){if(!(nn(c)&&(ne.tagNameCheck instanceof RegExp&&ye(ne.tagNameCheck,c)||ne.tagNameCheck instanceof Function&&ne.tagNameCheck(c))&&(ne.attributeNameCheck instanceof RegExp&&ye(ne.attributeNameCheck,f)||ne.attributeNameCheck instanceof Function&&ne.attributeNameCheck(f,c))||f==="is"&&ne.allowCustomizedBuiltInElements&&(ne.tagNameCheck instanceof RegExp&&ye(ne.tagNameCheck,S)||ne.tagNameCheck instanceof Function&&ne.tagNameCheck(S))))return!1}else if(!Jt[f]){if(!ye(be,on(S,H,""))){if(!((f==="src"||f==="xlink:href"||f==="href")&&c!=="script"&&Ua(S,"data:")===0&&Cn[c])){if(!(Tn&&!ye(mt,on(S,H,"")))){if(S)return!1}}}}}}}return!0},nn=function(c){return c!=="annotation-xml"&&xs(c,he)},vt=function(c){ze(ie.beforeSanitizeAttributes,c,null);const{attributes:f}=c;if(!f||tn(c))return;const S={attrName:"",attrValue:"",keepAttr:!0,allowedAttributes:J,forceKeepAttr:void 0};let X=f.length;for(;X--;){const pe=f[X],{name:re,namespaceURI:me,value:Se}=pe,ct=b(re),_=Se;let w=re==="value"?_:Ba(_);if(S.attrName=ct,S.attrValue=w,S.keepAttr=!0,S.forceKeepAttr=void 0,ze(ie.uponSanitizeAttribute,c,S),w=S.attrValue,Rn&&(ct==="id"||ct==="name")&&(Re(re,c),w=On+w),Mt&&ye(/((--!?|])>)|<\/(style|script|title|xmp|textarea|noscript|iframe|noembed|noframes)/i,w)){Re(re,c);continue}if(ct==="attributename"&&xs(w,"href")){Re(re,c);continue}if(S.forceKeepAttr)continue;if(!S.keepAttr){Re(re,c);continue}if(!An&&ye(/\/>/i,w)){Re(re,c);continue}rt&&Hn([Ve,Xe,Ae],K=>{w=on(w,K," ")});const M=b(c.nodeName);if(!Dn(M,ct,w)){Re(re,c);continue}if(z&&typeof y=="object"&&typeof y.getAttributeType=="function"&&!me)switch(y.getAttributeType(M,ct)){case"TrustedHTML":{w=z.createHTML(w);break}case"TrustedScriptURL":{w=z.createScriptURL(w);break}}if(w!==_)try{me?c.setAttributeNS(me,re,w):c.setAttribute(re,w),tn(c)?G(c):mi(e.removed)}catch{Re(re,c)}}ze(ie.afterSanitizeAttributes,c,null)},us=function A(c){let f=null;const S=Bt(c);for(ze(ie.beforeSanitizeShadowDOM,c,null);f=S.nextNode();)ze(ie.uponSanitizeShadowNode,f,null),Pn(f),vt(f),f.content instanceof a&&A(f.content);ze(ie.afterSanitizeShadowDOM,c,null)};return e.sanitize=function(A){let c=arguments.length>1&&arguments[1]!==void 0?arguments[1]:{},f=null,S=null,X=null,pe=null;if(en=!A,en&&(A="<!-->"),typeof A!="string"&&!Ht(A))if(typeof A.toString=="function"){if(A=A.toString(),typeof A!="string")throw ln("dirty is not a string, aborting")}else throw ln("toString is not a function");if(!e.isSupported)return A;if(Yt||k(c),e.removed=[],typeof A=="string"&&(ot=!1),ot){if(A.nodeName){const Se=b(A.nodeName);if(!q[Se]||_t[Se])throw ln("root node is forbidden and cannot be sanitized in-place")}}else if(A instanceof d)f=Ut("<!---->"),S=f.ownerDocument.importNode(A,!0),S.nodeType===dn.element&&S.nodeName==="BODY"||S.nodeName==="HTML"?f=S:f.appendChild(S);else{if(!at&&!rt&&!Qe&&A.indexOf("<")===-1)return z&&xt?z.createHTML(A):A;if(f=Ut(A),!f)return at?null:xt?Y:""}f&&Qt&&G(f.firstChild);const re=Bt(ot?A:f);for(;X=re.nextNode();)Pn(X),vt(X),X.content instanceof a&&us(X.content);if(ot)return A;if(at){if(Lt)for(pe=Le.call(f.ownerDocument);f.firstChild;)pe.appendChild(f.firstChild);else pe=f;return(J.shadowroot||J.shadowrootmode)&&(pe=st.call(i,pe,!0)),pe}let me=Qe?f.outerHTML:f.innerHTML;return Qe&&q["!doctype"]&&f.ownerDocument&&f.ownerDocument.doctype&&f.ownerDocument.doctype.name&&ye(Yi,f.ownerDocument.doctype.name)&&(me="<!DOCTYPE "+f.ownerDocument.doctype.name+`>
`+me),rt&&Hn([Ve,Xe,Ae],Se=>{me=on(me,Se," ")}),z&&xt?z.createHTML(me):me},e.setConfig=function(){let A=arguments.length>0&&arguments[0]!==void 0?arguments[0]:{};k(A),Yt=!0},e.clearConfig=function(){O=null,Yt=!1},e.isValidAttribute=function(A,c,f){O||k({});const S=b(A),X=b(c);return Dn(S,X,f)},e.addHook=function(A,c){typeof c=="function"&&an(ie[A],c)},e.removeHook=function(A,c){if(c!==void 0){const f=Da(ie[A],c);return f===-1?void 0:za(ie[A],f,1)[0]}return mi(ie[A])},e.removeHooks=function(A){ie[A]=[]},e.removeAllHooks=function(){ie=vi()},e}var to=Qi();function Ds(){return{async:!1,breaks:!1,extensions:null,gfm:!0,hooks:null,pedantic:!1,renderer:null,silent:!1,tokenizer:null,walkTokens:null}}var Ct=Ds();function Ji(t){Ct=t}var At={exec:()=>null};function U(t,e=""){let n=typeof t=="string"?t:t.source,i={replace:(s,a)=>{let r=typeof a=="string"?a:a.source;return r=r.replace(Ee.caret,"$1"),n=n.replace(s,r),i},getRegex:()=>new RegExp(n,e)};return i}var no=(()=>{try{return!!new RegExp("(?<=1)(?<!1)")}catch{return!1}})(),Ee={codeRemoveIndent:/^(?: {1,4}| {0,3}\t)/gm,outputLinkReplace:/\\([\[\]])/g,indentCodeCompensation:/^(\s+)(?:```)/,beginningSpace:/^\s+/,endingHash:/#$/,startingSpaceChar:/^ /,endingSpaceChar:/ $/,nonSpaceChar:/[^ ]/,newLineCharGlobal:/\n/g,tabCharGlobal:/\t/g,multipleSpaceGlobal:/\s+/g,blankLine:/^[ \t]*$/,doubleBlankLine:/\n[ \t]*\n[ \t]*$/,blockquoteStart:/^ {0,3}>/,blockquoteSetextReplace:/\n {0,3}((?:=+|-+) *)(?=\n|$)/g,blockquoteSetextReplace2:/^ {0,3}>[ \t]?/gm,listReplaceNesting:/^ {1,4}(?=( {4})*[^ ])/g,listIsTask:/^\[[ xX]\] +\S/,listReplaceTask:/^\[[ xX]\] +/,listTaskCheckbox:/\[[ xX]\]/,anyLine:/\n.*\n/,hrefBrackets:/^<(.*)>$/,tableDelimiter:/[:|]/,tableAlignChars:/^\||\| *$/g,tableRowBlankLine:/\n[ \t]*$/,tableAlignRight:/^ *-+: *$/,tableAlignCenter:/^ *:-+: *$/,tableAlignLeft:/^ *:-+ *$/,startATag:/^<a /i,endATag:/^<\/a>/i,startPreScriptTag:/^<(pre|code|kbd|script)(\s|>)/i,endPreScriptTag:/^<\/(pre|code|kbd|script)(\s|>)/i,startAngleBracket:/^</,endAngleBracket:/>$/,pedanticHrefTitle:/^([^'"]*[^\s])\s+(['"])(.*)\2/,unicodeAlphaNumeric:/[\p{L}\p{N}]/u,escapeTest:/[&<>"']/,escapeReplace:/[&<>"']/g,escapeTestNoEncode:/[<>"']|&(?!(#\d{1,7}|#[Xx][a-fA-F0-9]{1,6}|\w+);)/,escapeReplaceNoEncode:/[<>"']|&(?!(#\d{1,7}|#[Xx][a-fA-F0-9]{1,6}|\w+);)/g,unescapeTest:/&(#(?:\d+)|(?:#x[0-9A-Fa-f]+)|(?:\w+));?/ig,caret:/(^|[^\[])\^/g,percentDecode:/%25/g,findPipe:/\|/g,splitPipe:/ \|/,slashPipe:/\\\|/g,carriageReturn:/\r\n|\r/g,spaceLine:/^ +$/gm,notSpaceStart:/^\S*/,endingNewline:/\n$/,listItemRegex:t=>new RegExp(`^( {0,3}${t})((?:[	 ][^\\n]*)?(?:\\n|$))`),nextBulletRegex:t=>new RegExp(`^ {0,${Math.min(3,t-1)}}(?:[*+-]|\\d{1,9}[.)])((?:[ 	][^\\n]*)?(?:\\n|$))`),hrRegex:t=>new RegExp(`^ {0,${Math.min(3,t-1)}}((?:- *){3,}|(?:_ *){3,}|(?:\\* *){3,})(?:\\n+|$)`),fencesBeginRegex:t=>new RegExp(`^ {0,${Math.min(3,t-1)}}(?:\`\`\`|~~~)`),headingBeginRegex:t=>new RegExp(`^ {0,${Math.min(3,t-1)}}#`),htmlBeginRegex:t=>new RegExp(`^ {0,${Math.min(3,t-1)}}<(?:[a-z].*>|!--)`,"i"),blockquoteBeginRegex:t=>new RegExp(`^ {0,${Math.min(3,t-1)}}>`)},so=/^(?:[ \t]*(?:\n|$))+/,io=/^((?: {4}| {0,3}\t)[^\n]+(?:\n(?:[ \t]*(?:\n|$))*)?)+/,ro=/^ {0,3}(`{3,}(?=[^`\n]*(?:\n|$))|~{3,})([^\n]*)(?:\n|$)(?:|([\s\S]*?)(?:\n|$))(?: {0,3}\1[~`]* *(?=\n|$)|$)/,kn=/^ {0,3}((?:-[\t ]*){3,}|(?:_[ \t]*){3,}|(?:\*[ \t]*){3,})(?:\n+|$)/,ao=/^ {0,3}(#{1,6})(?=\s|$)(.*)(?:\n+|$)/,zs=/ {0,3}(?:[*+-]|\d{1,9}[.)])/,er=/^(?!bull |blockCode|fences|blockquote|heading|html|table)((?:.|\n(?!\s*?\n|bull |blockCode|fences|blockquote|heading|html|table))+?)\n {0,3}(=+|-+) *(?:\n+|$)/,tr=U(er).replace(/bull/g,zs).replace(/blockCode/g,/(?: {4}| {0,3}\t)/).replace(/fences/g,/ {0,3}(?:`{3,}|~{3,})/).replace(/blockquote/g,/ {0,3}>/).replace(/heading/g,/ {0,3}#{1,6}/).replace(/html/g,/ {0,3}<[^\n>]+>\n/).replace(/\|table/g,"").getRegex(),oo=U(er).replace(/bull/g,zs).replace(/blockCode/g,/(?: {4}| {0,3}\t)/).replace(/fences/g,/ {0,3}(?:`{3,}|~{3,})/).replace(/blockquote/g,/ {0,3}>/).replace(/heading/g,/ {0,3}#{1,6}/).replace(/html/g,/ {0,3}<[^\n>]+>\n/).replace(/table/g,/ {0,3}\|?(?:[:\- ]*\|)+[\:\- ]*\n/).getRegex(),Us=/^([^\n]+(?:\n(?!hr|heading|lheading|blockquote|fences|list|html|table| +\n)[^\n]+)*)/,lo=/^[^\n]+/,Bs=/(?!\s*\])(?:\\[\s\S]|[^\[\]\\])+/,co=U(/^ {0,3}\[(label)\]: *(?:\n[ \t]*)?([^<\s][^\s]*|<.*?>)(?:(?: +(?:\n[ \t]*)?| *\n[ \t]*)(title))? *(?:\n+|$)/).replace("label",Bs).replace("title",/(?:"(?:\\"?|[^"\\])*"|'[^'\n]*(?:\n[^'\n]+)*\n?'|\([^()]*\))/).getRegex(),po=U(/^(bull)([ \t][^\n]+?)?(?:\n|$)/).replace(/bull/g,zs).getRegex(),os="address|article|aside|base|basefont|blockquote|body|caption|center|col|colgroup|dd|details|dialog|dir|div|dl|dt|fieldset|figcaption|figure|footer|form|frame|frameset|h[1-6]|head|header|hr|html|iframe|legend|li|link|main|menu|menuitem|meta|nav|noframes|ol|optgroup|option|p|param|search|section|summary|table|tbody|td|tfoot|th|thead|title|tr|track|ul",Hs=/<!--(?:-?>|[\s\S]*?(?:-->|$))/,uo=U("^ {0,3}(?:<(script|pre|style|textarea)[\\s>][\\s\\S]*?(?:</\\1>[^\\n]*\\n+|$)|comment[^\\n]*(\\n+|$)|<\\?[\\s\\S]*?(?:\\?>\\n*|$)|<![A-Z][\\s\\S]*?(?:>\\n*|$)|<!\\[CDATA\\[[\\s\\S]*?(?:\\]\\]>\\n*|$)|</?(tag)(?: +|\\n|/?>)[\\s\\S]*?(?:(?:\\n[ 	]*)+\\n|$)|<(?!script|pre|style|textarea)([a-z][\\w-]*)(?:attribute)*? */?>(?=[ \\t]*(?:\\n|$))[\\s\\S]*?(?:(?:\\n[ 	]*)+\\n|$)|</(?!script|pre|style|textarea)[a-z][\\w-]*\\s*>(?=[ \\t]*(?:\\n|$))[\\s\\S]*?(?:(?:\\n[ 	]*)+\\n|$))","i").replace("comment",Hs).replace("tag",os).replace("attribute",/ +[a-zA-Z:_][\w.:-]*(?: *= *"[^"\n]*"| *= *'[^'\n]*'| *= *[^\s"'=<>`]+)?/).getRegex(),nr=U(Us).replace("hr",kn).replace("heading"," {0,3}#{1,6}(?:\\s|$)").replace("|lheading","").replace("|table","").replace("blockquote"," {0,3}>").replace("fences"," {0,3}(?:`{3,}(?=[^`\\n]*\\n)|~{3,})[^\\n]*\\n").replace("list"," {0,3}(?:[*+-]|1[.)])[ \\t]").replace("html","</?(?:tag)(?: +|\\n|/?>)|<(?:script|pre|style|textarea|!--)").replace("tag",os).getRegex(),ho=U(/^( {0,3}> ?(paragraph|[^\n]*)(?:\n|$))+/).replace("paragraph",nr).getRegex(),Fs={blockquote:ho,code:io,def:co,fences:ro,heading:ao,hr:kn,html:uo,lheading:tr,list:po,newline:so,paragraph:nr,table:At,text:lo},ki=U("^ *([^\\n ].*)\\n {0,3}((?:\\| *)?:?-+:? *(?:\\| *:?-+:? *)*(?:\\| *)?)(?:\\n((?:(?! *\\n|hr|heading|blockquote|code|fences|list|html).*(?:\\n|$))*)\\n*|$)").replace("hr",kn).replace("heading"," {0,3}#{1,6}(?:\\s|$)").replace("blockquote"," {0,3}>").replace("code","(?: {4}| {0,3}	)[^\\n]").replace("fences"," {0,3}(?:`{3,}(?=[^`\\n]*\\n)|~{3,})[^\\n]*\\n").replace("list"," {0,3}(?:[*+-]|1[.)])[ \\t]").replace("html","</?(?:tag)(?: +|\\n|/?>)|<(?:script|pre|style|textarea|!--)").replace("tag",os).getRegex(),go={...Fs,lheading:oo,table:ki,paragraph:U(Us).replace("hr",kn).replace("heading"," {0,3}#{1,6}(?:\\s|$)").replace("|lheading","").replace("table",ki).replace("blockquote"," {0,3}>").replace("fences"," {0,3}(?:`{3,}(?=[^`\\n]*\\n)|~{3,})[^\\n]*\\n").replace("list"," {0,3}(?:[*+-]|1[.)])[ \\t]").replace("html","</?(?:tag)(?: +|\\n|/?>)|<(?:script|pre|style|textarea|!--)").replace("tag",os).getRegex()},fo={...Fs,html:U(`^ *(?:comment *(?:\\n|\\s*$)|<(tag)[\\s\\S]+?</\\1> *(?:\\n{2,}|\\s*$)|<tag(?:"[^"]*"|'[^']*'|\\s[^'"/>\\s]*)*?/?> *(?:\\n{2,}|\\s*$))`).replace("comment",Hs).replace(/tag/g,"(?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\\b)\\w+(?!:|[^\\w\\s@]*@)\\b").getRegex(),def:/^ *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +(["(][^\n]+[")]))? *(?:\n+|$)/,heading:/^(#{1,6})(.*)(?:\n+|$)/,fences:At,lheading:/^(.+?)\n {0,3}(=+|-+) *(?:\n+|$)/,paragraph:U(Us).replace("hr",kn).replace("heading",` *#{1,6} *[^
]`).replace("lheading",tr).replace("|table","").replace("blockquote"," {0,3}>").replace("|fences","").replace("|list","").replace("|html","").replace("|tag","").getRegex()},bo=/^\\([!"#$%&'()*+,\-./:;<=>?@\[\]\\^_`{|}~])/,mo=/^(`+)([^`]|[^`][\s\S]*?[^`])\1(?!`)/,sr=/^( {2,}|\\)\n(?!\s*$)/,_o=/^(`+|[^`])(?:(?= {2,}\n)|[\s\S]*?(?:(?=[\\<!\[`*_]|\b_|$)|[^ ](?= {2,}\n)))/,ls=/[\p{P}\p{S}]/u,Gs=/[\s\p{P}\p{S}]/u,ir=/[^\s\p{P}\p{S}]/u,xo=U(/^((?![*_])punctSpace)/,"u").replace(/punctSpace/g,Gs).getRegex(),rr=/(?!~)[\p{P}\p{S}]/u,yo=/(?!~)[\s\p{P}\p{S}]/u,wo=/(?:[^\s\p{P}\p{S}]|~)/u,ar=/(?![*_])[\p{P}\p{S}]/u,Eo=/(?![*_])[\s\p{P}\p{S}]/u,vo=/(?:[^\s\p{P}\p{S}]|[*_])/u,ko=U(/link|precode-code|html/,"g").replace("link",/\[(?:[^\[\]`]|(?<a>`+)[^`]+\k<a>(?!`))*?\]\((?:\\[\s\S]|[^\\\(\)]|\((?:\\[\s\S]|[^\\\(\)])*\))*\)/).replace("precode-",no?"(?<!`)()":"(^^|[^`])").replace("code",/(?<b>`+)[^`]+\k<b>(?!`)/).replace("html",/<(?! )[^<>]*?>/).getRegex(),or=/^(?:\*+(?:((?!\*)punct)|[^\s*]))|^_+(?:((?!_)punct)|([^\s_]))/,So=U(or,"u").replace(/punct/g,ls).getRegex(),To=U(or,"u").replace(/punct/g,rr).getRegex(),lr="^[^_*]*?__[^_*]*?\\*[^_*]*?(?=__)|[^*]+(?=[^*])|(?!\\*)punct(\\*+)(?=[\\s]|$)|notPunctSpace(\\*+)(?!\\*)(?=punctSpace|$)|(?!\\*)punctSpace(\\*+)(?=notPunctSpace)|[\\s](\\*+)(?!\\*)(?=punct)|(?!\\*)punct(\\*+)(?!\\*)(?=punct)|notPunctSpace(\\*+)(?=notPunctSpace)",Ao=U(lr,"gu").replace(/notPunctSpace/g,ir).replace(/punctSpace/g,Gs).replace(/punct/g,ls).getRegex(),$o=U(lr,"gu").replace(/notPunctSpace/g,wo).replace(/punctSpace/g,yo).replace(/punct/g,rr).getRegex(),Ro=U("^[^_*]*?\\*\\*[^_*]*?_[^_*]*?(?=\\*\\*)|[^_]+(?=[^_])|(?!_)punct(_+)(?=[\\s]|$)|notPunctSpace(_+)(?!_)(?=punctSpace|$)|(?!_)punctSpace(_+)(?=notPunctSpace)|[\\s](_+)(?!_)(?=punct)|(?!_)punct(_+)(?!_)(?=punct)","gu").replace(/notPunctSpace/g,ir).replace(/punctSpace/g,Gs).replace(/punct/g,ls).getRegex(),Oo=U(/^~~?(?:((?!~)punct)|[^\s~])/,"u").replace(/punct/g,ar).getRegex(),Co="^[^~]+(?=[^~])|(?!~)punct(~~?)(?=[\\s]|$)|notPunctSpace(~~?)(?!~)(?=punctSpace|$)|(?!~)punctSpace(~~?)(?=notPunctSpace)|[\\s](~~?)(?!~)(?=punct)|(?!~)punct(~~?)(?!~)(?=punct)|notPunctSpace(~~?)(?=notPunctSpace)",No=U(Co,"gu").replace(/notPunctSpace/g,vo).replace(/punctSpace/g,Eo).replace(/punct/g,ar).getRegex(),Io=U(/\\(punct)/,"gu").replace(/punct/g,ls).getRegex(),Mo=U(/^<(scheme:[^\s\x00-\x1f<>]*|email)>/).replace("scheme",/[a-zA-Z][a-zA-Z0-9+.-]{1,31}/).replace("email",/[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+(@)[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+(?![-_])/).getRegex(),Lo=U(Hs).replace("(?:-->|$)","-->").getRegex(),Po=U("^comment|^</[a-zA-Z][\\w:-]*\\s*>|^<[a-zA-Z][\\w-]*(?:attribute)*?\\s*/?>|^<\\?[\\s\\S]*?\\?>|^<![a-zA-Z]+\\s[\\s\\S]*?>|^<!\\[CDATA\\[[\\s\\S]*?\\]\\]>").replace("comment",Lo).replace("attribute",/\s+[a-zA-Z:_][\w.:-]*(?:\s*=\s*"[^"]*"|\s*=\s*'[^']*'|\s*=\s*[^\s"'=<>`]+)?/).getRegex(),Yn=/(?:\[(?:\\[\s\S]|[^\[\]\\])*\]|\\[\s\S]|`+[^`]*?`+(?!`)|[^\[\]\\`])*?/,Do=U(/^!?\[(label)\]\(\s*(href)(?:(?:[ \t]*(?:\n[ \t]*)?)(title))?\s*\)/).replace("label",Yn).replace("href",/<(?:\\.|[^\n<>\\])+>|[^ \t\n\x00-\x1f]*/).replace("title",/"(?:\\"?|[^"\\])*"|'(?:\\'?|[^'\\])*'|\((?:\\\)?|[^)\\])*\)/).getRegex(),cr=U(/^!?\[(label)\]\[(ref)\]/).replace("label",Yn).replace("ref",Bs).getRegex(),dr=U(/^!?\[(ref)\](?:\[\])?/).replace("ref",Bs).getRegex(),zo=U("reflink|nolink(?!\\()","g").replace("reflink",cr).replace("nolink",dr).getRegex(),Si=/[hH][tT][tT][pP][sS]?|[fF][tT][pP]/,Ws={_backpedal:At,anyPunctuation:Io,autolink:Mo,blockSkip:ko,br:sr,code:mo,del:At,delLDelim:At,delRDelim:At,emStrongLDelim:So,emStrongRDelimAst:Ao,emStrongRDelimUnd:Ro,escape:bo,link:Do,nolink:dr,punctuation:xo,reflink:cr,reflinkSearch:zo,tag:Po,text:_o,url:At},Uo={...Ws,link:U(/^!?\[(label)\]\((.*?)\)/).replace("label",Yn).getRegex(),reflink:U(/^!?\[(label)\]\s*\[([^\]]*)\]/).replace("label",Yn).getRegex()},$s={...Ws,emStrongRDelimAst:$o,emStrongLDelim:To,delLDelim:Oo,delRDelim:No,url:U(/^((?:protocol):\/\/|www\.)(?:[a-zA-Z0-9\-]+\.?)+[^\s<]*|^email/).replace("protocol",Si).replace("email",/[A-Za-z0-9._+-]+(@)[a-zA-Z0-9-_]+(?:\.[a-zA-Z0-9-_]*[a-zA-Z0-9])+(?![-_])/).getRegex(),_backpedal:/(?:[^?!.,:;*_'"~()&]+|\([^)]*\)|&(?![a-zA-Z0-9]+;$)|[?!.,:;*_'"~)]+(?!$))+/,del:/^(~~?)(?=[^\s~])((?:\\[\s\S]|[^\\])*?(?:\\[\s\S]|[^\s~\\]))\1(?=[^~]|$)/,text:U(/^([`~]+|[^`~])(?:(?= {2,}\n)|(?=[a-zA-Z0-9.!#$%&'*+\/=?_`{\|}~-]+@)|[\s\S]*?(?:(?=[\\<!\[`*~_]|\b_|protocol:\/\/|www\.|$)|[^ ](?= {2,}\n)|[^a-zA-Z0-9.!#$%&'*+\/=?_`{\|}~-](?=[a-zA-Z0-9.!#$%&'*+\/=?_`{\|}~-]+@)))/).replace("protocol",Si).getRegex()},Bo={...$s,br:U(sr).replace("{2,}","*").getRegex(),text:U($s.text).replace("\\b_","\\b_| {2,}\\n").replace(/\{2,\}/g,"*").getRegex()},Gn={normal:Fs,gfm:go,pedantic:fo},pn={normal:Ws,gfm:$s,breaks:Bo,pedantic:Uo},Ho={"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"},Ti=t=>Ho[t];function qe(t,e){if(e){if(Ee.escapeTest.test(t))return t.replace(Ee.escapeReplace,Ti)}else if(Ee.escapeTestNoEncode.test(t))return t.replace(Ee.escapeReplaceNoEncode,Ti);return t}function Ai(t){try{t=encodeURI(t).replace(Ee.percentDecode,"%")}catch{return null}return t}function $i(t,e){let n=t.replace(Ee.findPipe,(a,r,d)=>{let o=!1,p=r;for(;--p>=0&&d[p]==="\\";)o=!o;return o?"|":" |"}),i=n.split(Ee.splitPipe),s=0;if(i[0].trim()||i.shift(),i.length>0&&!i.at(-1)?.trim()&&i.pop(),e)if(i.length>e)i.splice(e);else for(;i.length<e;)i.push("");for(;s<i.length;s++)i[s]=i[s].trim().replace(Ee.slashPipe,"|");return i}function un(t,e,n){let i=t.length;if(i===0)return"";let s=0;for(;s<i&&t.charAt(i-s-1)===e;)s++;return t.slice(0,i-s)}function Fo(t,e){if(t.indexOf(e[1])===-1)return-1;let n=0;for(let i=0;i<t.length;i++)if(t[i]==="\\")i++;else if(t[i]===e[0])n++;else if(t[i]===e[1]&&(n--,n<0))return i;return n>0?-2:-1}function Go(t,e=0){let n=e,i="";for(let s of t)if(s==="	"){let a=4-n%4;i+=" ".repeat(a),n+=a}else i+=s,n++;return i}function Ri(t,e,n,i,s){let a=e.href,r=e.title||null,d=t[1].replace(s.other.outputLinkReplace,"$1");i.state.inLink=!0;let o={type:t[0].charAt(0)==="!"?"image":"link",raw:n,href:a,title:r,text:d,tokens:i.inlineTokens(d)};return i.state.inLink=!1,o}function Wo(t,e,n){let i=t.match(n.other.indentCodeCompensation);if(i===null)return e;let s=i[1];return e.split(`
`).map(a=>{let r=a.match(n.other.beginningSpace);if(r===null)return a;let[d]=r;return d.length>=s.length?a.slice(s.length):a}).join(`
`)}var Qn=class{constructor(t){V(this,"options");V(this,"rules");V(this,"lexer");this.options=t||Ct}space(t){let e=this.rules.block.newline.exec(t);if(e&&e[0].length>0)return{type:"space",raw:e[0]}}code(t){let e=this.rules.block.code.exec(t);if(e){let n=e[0].replace(this.rules.other.codeRemoveIndent,"");return{type:"code",raw:e[0],codeBlockStyle:"indented",text:this.options.pedantic?n:un(n,`
`)}}}fences(t){let e=this.rules.block.fences.exec(t);if(e){let n=e[0],i=Wo(n,e[3]||"",this.rules);return{type:"code",raw:n,lang:e[2]?e[2].trim().replace(this.rules.inline.anyPunctuation,"$1"):e[2],text:i}}}heading(t){let e=this.rules.block.heading.exec(t);if(e){let n=e[2].trim();if(this.rules.other.endingHash.test(n)){let i=un(n,"#");(this.options.pedantic||!i||this.rules.other.endingSpaceChar.test(i))&&(n=i.trim())}return{type:"heading",raw:e[0],depth:e[1].length,text:n,tokens:this.lexer.inline(n)}}}hr(t){let e=this.rules.block.hr.exec(t);if(e)return{type:"hr",raw:un(e[0],`
`)}}blockquote(t){let e=this.rules.block.blockquote.exec(t);if(e){let n=un(e[0],`
`).split(`
`),i="",s="",a=[];for(;n.length>0;){let r=!1,d=[],o;for(o=0;o<n.length;o++)if(this.rules.other.blockquoteStart.test(n[o]))d.push(n[o]),r=!0;else if(!r)d.push(n[o]);else break;n=n.slice(o);let p=d.join(`
`),u=p.replace(this.rules.other.blockquoteSetextReplace,`
    $1`).replace(this.rules.other.blockquoteSetextReplace2,"");i=i?`${i}
${p}`:p,s=s?`${s}
${u}`:u;let g=this.lexer.state.top;if(this.lexer.state.top=!0,this.lexer.blockTokens(u,a,!0),this.lexer.state.top=g,n.length===0)break;let m=a.at(-1);if(m?.type==="code")break;if(m?.type==="blockquote"){let y=m,E=y.raw+`
`+n.join(`
`),R=this.blockquote(E);a[a.length-1]=R,i=i.substring(0,i.length-y.raw.length)+R.raw,s=s.substring(0,s.length-y.text.length)+R.text;break}else if(m?.type==="list"){let y=m,E=y.raw+`
`+n.join(`
`),R=this.list(E);a[a.length-1]=R,i=i.substring(0,i.length-m.raw.length)+R.raw,s=s.substring(0,s.length-y.raw.length)+R.raw,n=E.substring(a.at(-1).raw.length).split(`
`);continue}}return{type:"blockquote",raw:i,tokens:a,text:s}}}list(t){let e=this.rules.block.list.exec(t);if(e){let n=e[1].trim(),i=n.length>1,s={type:"list",raw:"",ordered:i,start:i?+n.slice(0,-1):"",loose:!1,items:[]};n=i?`\\d{1,9}\\${n.slice(-1)}`:`\\${n}`,this.options.pedantic&&(n=i?n:"[*+-]");let a=this.rules.other.listItemRegex(n),r=!1;for(;t;){let o=!1,p="",u="";if(!(e=a.exec(t))||this.rules.block.hr.test(t))break;p=e[0],t=t.substring(p.length);let g=Go(e[2].split(`
`,1)[0],e[1].length),m=t.split(`
`,1)[0],y=!g.trim(),E=0;if(this.options.pedantic?(E=2,u=g.trimStart()):y?E=e[1].length+1:(E=g.search(this.rules.other.nonSpaceChar),E=E>4?1:E,u=g.slice(E),E+=e[1].length),y&&this.rules.other.blankLine.test(m)&&(p+=m+`
`,t=t.substring(m.length+1),o=!0),!o){let R=this.rules.other.nextBulletRegex(E),B=this.rules.other.hrRegex(E),j=this.rules.other.fencesBeginRegex(E),P=this.rules.other.headingBeginRegex(E),D=this.rules.other.htmlBeginRegex(E),z=this.rules.other.blockquoteBeginRegex(E);for(;t;){let Y=t.split(`
`,1)[0],te;if(m=Y,this.options.pedantic?(m=m.replace(this.rules.other.listReplaceNesting,"  "),te=m):te=m.replace(this.rules.other.tabCharGlobal,"    "),j.test(m)||P.test(m)||D.test(m)||z.test(m)||R.test(m)||B.test(m))break;if(te.search(this.rules.other.nonSpaceChar)>=E||!m.trim())u+=`
`+te.slice(E);else{if(y||g.replace(this.rules.other.tabCharGlobal,"    ").search(this.rules.other.nonSpaceChar)>=4||j.test(g)||P.test(g)||B.test(g))break;u+=`
`+m}y=!m.trim(),p+=Y+`
`,t=t.substring(Y.length+1),g=te.slice(E)}}s.loose||(r?s.loose=!0:this.rules.other.doubleBlankLine.test(p)&&(r=!0)),s.items.push({type:"list_item",raw:p,task:!!this.options.gfm&&this.rules.other.listIsTask.test(u),loose:!1,text:u,tokens:[]}),s.raw+=p}let d=s.items.at(-1);if(d)d.raw=d.raw.trimEnd(),d.text=d.text.trimEnd();else return;s.raw=s.raw.trimEnd();for(let o of s.items){if(this.lexer.state.top=!1,o.tokens=this.lexer.blockTokens(o.text,[]),o.task){if(o.text=o.text.replace(this.rules.other.listReplaceTask,""),o.tokens[0]?.type==="text"||o.tokens[0]?.type==="paragraph"){o.tokens[0].raw=o.tokens[0].raw.replace(this.rules.other.listReplaceTask,""),o.tokens[0].text=o.tokens[0].text.replace(this.rules.other.listReplaceTask,"");for(let u=this.lexer.inlineQueue.length-1;u>=0;u--)if(this.rules.other.listIsTask.test(this.lexer.inlineQueue[u].src)){this.lexer.inlineQueue[u].src=this.lexer.inlineQueue[u].src.replace(this.rules.other.listReplaceTask,"");break}}let p=this.rules.other.listTaskCheckbox.exec(o.raw);if(p){let u={type:"checkbox",raw:p[0]+" ",checked:p[0]!=="[ ]"};o.checked=u.checked,s.loose?o.tokens[0]&&["paragraph","text"].includes(o.tokens[0].type)&&"tokens"in o.tokens[0]&&o.tokens[0].tokens?(o.tokens[0].raw=u.raw+o.tokens[0].raw,o.tokens[0].text=u.raw+o.tokens[0].text,o.tokens[0].tokens.unshift(u)):o.tokens.unshift({type:"paragraph",raw:u.raw,text:u.raw,tokens:[u]}):o.tokens.unshift(u)}}if(!s.loose){let p=o.tokens.filter(g=>g.type==="space"),u=p.length>0&&p.some(g=>this.rules.other.anyLine.test(g.raw));s.loose=u}}if(s.loose)for(let o of s.items){o.loose=!0;for(let p of o.tokens)p.type==="text"&&(p.type="paragraph")}return s}}html(t){let e=this.rules.block.html.exec(t);if(e)return{type:"html",block:!0,raw:e[0],pre:e[1]==="pre"||e[1]==="script"||e[1]==="style",text:e[0]}}def(t){let e=this.rules.block.def.exec(t);if(e){let n=e[1].toLowerCase().replace(this.rules.other.multipleSpaceGlobal," "),i=e[2]?e[2].replace(this.rules.other.hrefBrackets,"$1").replace(this.rules.inline.anyPunctuation,"$1"):"",s=e[3]?e[3].substring(1,e[3].length-1).replace(this.rules.inline.anyPunctuation,"$1"):e[3];return{type:"def",tag:n,raw:e[0],href:i,title:s}}}table(t){let e=this.rules.block.table.exec(t);if(!e||!this.rules.other.tableDelimiter.test(e[2]))return;let n=$i(e[1]),i=e[2].replace(this.rules.other.tableAlignChars,"").split("|"),s=e[3]?.trim()?e[3].replace(this.rules.other.tableRowBlankLine,"").split(`
`):[],a={type:"table",raw:e[0],header:[],align:[],rows:[]};if(n.length===i.length){for(let r of i)this.rules.other.tableAlignRight.test(r)?a.align.push("right"):this.rules.other.tableAlignCenter.test(r)?a.align.push("center"):this.rules.other.tableAlignLeft.test(r)?a.align.push("left"):a.align.push(null);for(let r=0;r<n.length;r++)a.header.push({text:n[r],tokens:this.lexer.inline(n[r]),header:!0,align:a.align[r]});for(let r of s)a.rows.push($i(r,a.header.length).map((d,o)=>({text:d,tokens:this.lexer.inline(d),header:!1,align:a.align[o]})));return a}}lheading(t){let e=this.rules.block.lheading.exec(t);if(e)return{type:"heading",raw:e[0],depth:e[2].charAt(0)==="="?1:2,text:e[1],tokens:this.lexer.inline(e[1])}}paragraph(t){let e=this.rules.block.paragraph.exec(t);if(e){let n=e[1].charAt(e[1].length-1)===`
`?e[1].slice(0,-1):e[1];return{type:"paragraph",raw:e[0],text:n,tokens:this.lexer.inline(n)}}}text(t){let e=this.rules.block.text.exec(t);if(e)return{type:"text",raw:e[0],text:e[0],tokens:this.lexer.inline(e[0])}}escape(t){let e=this.rules.inline.escape.exec(t);if(e)return{type:"escape",raw:e[0],text:e[1]}}tag(t){let e=this.rules.inline.tag.exec(t);if(e)return!this.lexer.state.inLink&&this.rules.other.startATag.test(e[0])?this.lexer.state.inLink=!0:this.lexer.state.inLink&&this.rules.other.endATag.test(e[0])&&(this.lexer.state.inLink=!1),!this.lexer.state.inRawBlock&&this.rules.other.startPreScriptTag.test(e[0])?this.lexer.state.inRawBlock=!0:this.lexer.state.inRawBlock&&this.rules.other.endPreScriptTag.test(e[0])&&(this.lexer.state.inRawBlock=!1),{type:"html",raw:e[0],inLink:this.lexer.state.inLink,inRawBlock:this.lexer.state.inRawBlock,block:!1,text:e[0]}}link(t){let e=this.rules.inline.link.exec(t);if(e){let n=e[2].trim();if(!this.options.pedantic&&this.rules.other.startAngleBracket.test(n)){if(!this.rules.other.endAngleBracket.test(n))return;let a=un(n.slice(0,-1),"\\");if((n.length-a.length)%2===0)return}else{let a=Fo(e[2],"()");if(a===-2)return;if(a>-1){let r=(e[0].indexOf("!")===0?5:4)+e[1].length+a;e[2]=e[2].substring(0,a),e[0]=e[0].substring(0,r).trim(),e[3]=""}}let i=e[2],s="";if(this.options.pedantic){let a=this.rules.other.pedanticHrefTitle.exec(i);a&&(i=a[1],s=a[3])}else s=e[3]?e[3].slice(1,-1):"";return i=i.trim(),this.rules.other.startAngleBracket.test(i)&&(this.options.pedantic&&!this.rules.other.endAngleBracket.test(n)?i=i.slice(1):i=i.slice(1,-1)),Ri(e,{href:i&&i.replace(this.rules.inline.anyPunctuation,"$1"),title:s&&s.replace(this.rules.inline.anyPunctuation,"$1")},e[0],this.lexer,this.rules)}}reflink(t,e){let n;if((n=this.rules.inline.reflink.exec(t))||(n=this.rules.inline.nolink.exec(t))){let i=(n[2]||n[1]).replace(this.rules.other.multipleSpaceGlobal," "),s=e[i.toLowerCase()];if(!s){let a=n[0].charAt(0);return{type:"text",raw:a,text:a}}return Ri(n,s,n[0],this.lexer,this.rules)}}emStrong(t,e,n=""){let i=this.rules.inline.emStrongLDelim.exec(t);if(!(!i||i[3]&&n.match(this.rules.other.unicodeAlphaNumeric))&&(!(i[1]||i[2])||!n||this.rules.inline.punctuation.exec(n))){let s=[...i[0]].length-1,a,r,d=s,o=0,p=i[0][0]==="*"?this.rules.inline.emStrongRDelimAst:this.rules.inline.emStrongRDelimUnd;for(p.lastIndex=0,e=e.slice(-1*t.length+s);(i=p.exec(e))!=null;){if(a=i[1]||i[2]||i[3]||i[4]||i[5]||i[6],!a)continue;if(r=[...a].length,i[3]||i[4]){d+=r;continue}else if((i[5]||i[6])&&s%3&&!((s+r)%3)){o+=r;continue}if(d-=r,d>0)continue;r=Math.min(r,r+d+o);let u=[...i[0]][0].length,g=t.slice(0,s+i.index+u+r);if(Math.min(s,r)%2){let y=g.slice(1,-1);return{type:"em",raw:g,text:y,tokens:this.lexer.inlineTokens(y)}}let m=g.slice(2,-2);return{type:"strong",raw:g,text:m,tokens:this.lexer.inlineTokens(m)}}}}codespan(t){let e=this.rules.inline.code.exec(t);if(e){let n=e[2].replace(this.rules.other.newLineCharGlobal," "),i=this.rules.other.nonSpaceChar.test(n),s=this.rules.other.startingSpaceChar.test(n)&&this.rules.other.endingSpaceChar.test(n);return i&&s&&(n=n.substring(1,n.length-1)),{type:"codespan",raw:e[0],text:n}}}br(t){let e=this.rules.inline.br.exec(t);if(e)return{type:"br",raw:e[0]}}del(t,e,n=""){let i=this.rules.inline.delLDelim.exec(t);if(i&&(!i[1]||!n||this.rules.inline.punctuation.exec(n))){let s=[...i[0]].length-1,a,r,d=s,o=this.rules.inline.delRDelim;for(o.lastIndex=0,e=e.slice(-1*t.length+s);(i=o.exec(e))!=null;){if(a=i[1]||i[2]||i[3]||i[4]||i[5]||i[6],!a||(r=[...a].length,r!==s))continue;if(i[3]||i[4]){d+=r;continue}if(d-=r,d>0)continue;r=Math.min(r,r+d);let p=[...i[0]][0].length,u=t.slice(0,s+i.index+p+r),g=u.slice(s,-s);return{type:"del",raw:u,text:g,tokens:this.lexer.inlineTokens(g)}}}}autolink(t){let e=this.rules.inline.autolink.exec(t);if(e){let n,i;return e[2]==="@"?(n=e[1],i="mailto:"+n):(n=e[1],i=n),{type:"link",raw:e[0],text:n,href:i,tokens:[{type:"text",raw:n,text:n}]}}}url(t){let e;if(e=this.rules.inline.url.exec(t)){let n,i;if(e[2]==="@")n=e[0],i="mailto:"+n;else{let s;do s=e[0],e[0]=this.rules.inline._backpedal.exec(e[0])?.[0]??"";while(s!==e[0]);n=e[0],e[1]==="www."?i="http://"+e[0]:i=e[0]}return{type:"link",raw:e[0],text:n,href:i,tokens:[{type:"text",raw:n,text:n}]}}}inlineText(t){let e=this.rules.inline.text.exec(t);if(e){let n=this.lexer.state.inRawBlock;return{type:"text",raw:e[0],text:e[0],escaped:n}}}},Be=class Rs{constructor(e){V(this,"tokens");V(this,"options");V(this,"state");V(this,"inlineQueue");V(this,"tokenizer");this.tokens=[],this.tokens.links=Object.create(null),this.options=e||Ct,this.options.tokenizer=this.options.tokenizer||new Qn,this.tokenizer=this.options.tokenizer,this.tokenizer.options=this.options,this.tokenizer.lexer=this,this.inlineQueue=[],this.state={inLink:!1,inRawBlock:!1,top:!0};let n={other:Ee,block:Gn.normal,inline:pn.normal};this.options.pedantic?(n.block=Gn.pedantic,n.inline=pn.pedantic):this.options.gfm&&(n.block=Gn.gfm,this.options.breaks?n.inline=pn.breaks:n.inline=pn.gfm),this.tokenizer.rules=n}static get rules(){return{block:Gn,inline:pn}}static lex(e,n){return new Rs(n).lex(e)}static lexInline(e,n){return new Rs(n).inlineTokens(e)}lex(e){e=e.replace(Ee.carriageReturn,`
`),this.blockTokens(e,this.tokens);for(let n=0;n<this.inlineQueue.length;n++){let i=this.inlineQueue[n];this.inlineTokens(i.src,i.tokens)}return this.inlineQueue=[],this.tokens}blockTokens(e,n=[],i=!1){for(this.options.pedantic&&(e=e.replace(Ee.tabCharGlobal,"    ").replace(Ee.spaceLine,""));e;){let s;if(this.options.extensions?.block?.some(r=>(s=r.call({lexer:this},e,n))?(e=e.substring(s.raw.length),n.push(s),!0):!1))continue;if(s=this.tokenizer.space(e)){e=e.substring(s.raw.length);let r=n.at(-1);s.raw.length===1&&r!==void 0?r.raw+=`
`:n.push(s);continue}if(s=this.tokenizer.code(e)){e=e.substring(s.raw.length);let r=n.at(-1);r?.type==="paragraph"||r?.type==="text"?(r.raw+=(r.raw.endsWith(`
`)?"":`
`)+s.raw,r.text+=`
`+s.text,this.inlineQueue.at(-1).src=r.text):n.push(s);continue}if(s=this.tokenizer.fences(e)){e=e.substring(s.raw.length),n.push(s);continue}if(s=this.tokenizer.heading(e)){e=e.substring(s.raw.length),n.push(s);continue}if(s=this.tokenizer.hr(e)){e=e.substring(s.raw.length),n.push(s);continue}if(s=this.tokenizer.blockquote(e)){e=e.substring(s.raw.length),n.push(s);continue}if(s=this.tokenizer.list(e)){e=e.substring(s.raw.length),n.push(s);continue}if(s=this.tokenizer.html(e)){e=e.substring(s.raw.length),n.push(s);continue}if(s=this.tokenizer.def(e)){e=e.substring(s.raw.length);let r=n.at(-1);r?.type==="paragraph"||r?.type==="text"?(r.raw+=(r.raw.endsWith(`
`)?"":`
`)+s.raw,r.text+=`
`+s.raw,this.inlineQueue.at(-1).src=r.text):this.tokens.links[s.tag]||(this.tokens.links[s.tag]={href:s.href,title:s.title},n.push(s));continue}if(s=this.tokenizer.table(e)){e=e.substring(s.raw.length),n.push(s);continue}if(s=this.tokenizer.lheading(e)){e=e.substring(s.raw.length),n.push(s);continue}let a=e;if(this.options.extensions?.startBlock){let r=1/0,d=e.slice(1),o;this.options.extensions.startBlock.forEach(p=>{o=p.call({lexer:this},d),typeof o=="number"&&o>=0&&(r=Math.min(r,o))}),r<1/0&&r>=0&&(a=e.substring(0,r+1))}if(this.state.top&&(s=this.tokenizer.paragraph(a))){let r=n.at(-1);i&&r?.type==="paragraph"?(r.raw+=(r.raw.endsWith(`
`)?"":`
`)+s.raw,r.text+=`
`+s.text,this.inlineQueue.pop(),this.inlineQueue.at(-1).src=r.text):n.push(s),i=a.length!==e.length,e=e.substring(s.raw.length);continue}if(s=this.tokenizer.text(e)){e=e.substring(s.raw.length);let r=n.at(-1);r?.type==="text"?(r.raw+=(r.raw.endsWith(`
`)?"":`
`)+s.raw,r.text+=`
`+s.text,this.inlineQueue.pop(),this.inlineQueue.at(-1).src=r.text):n.push(s);continue}if(e){let r="Infinite loop on byte: "+e.charCodeAt(0);if(this.options.silent){console.error(r);break}else throw new Error(r)}}return this.state.top=!0,n}inline(e,n=[]){return this.inlineQueue.push({src:e,tokens:n}),n}inlineTokens(e,n=[]){let i=e,s=null;if(this.tokens.links){let o=Object.keys(this.tokens.links);if(o.length>0)for(;(s=this.tokenizer.rules.inline.reflinkSearch.exec(i))!=null;)o.includes(s[0].slice(s[0].lastIndexOf("[")+1,-1))&&(i=i.slice(0,s.index)+"["+"a".repeat(s[0].length-2)+"]"+i.slice(this.tokenizer.rules.inline.reflinkSearch.lastIndex))}for(;(s=this.tokenizer.rules.inline.anyPunctuation.exec(i))!=null;)i=i.slice(0,s.index)+"++"+i.slice(this.tokenizer.rules.inline.anyPunctuation.lastIndex);let a;for(;(s=this.tokenizer.rules.inline.blockSkip.exec(i))!=null;)a=s[2]?s[2].length:0,i=i.slice(0,s.index+a)+"["+"a".repeat(s[0].length-a-2)+"]"+i.slice(this.tokenizer.rules.inline.blockSkip.lastIndex);i=this.options.hooks?.emStrongMask?.call({lexer:this},i)??i;let r=!1,d="";for(;e;){r||(d=""),r=!1;let o;if(this.options.extensions?.inline?.some(u=>(o=u.call({lexer:this},e,n))?(e=e.substring(o.raw.length),n.push(o),!0):!1))continue;if(o=this.tokenizer.escape(e)){e=e.substring(o.raw.length),n.push(o);continue}if(o=this.tokenizer.tag(e)){e=e.substring(o.raw.length),n.push(o);continue}if(o=this.tokenizer.link(e)){e=e.substring(o.raw.length),n.push(o);continue}if(o=this.tokenizer.reflink(e,this.tokens.links)){e=e.substring(o.raw.length);let u=n.at(-1);o.type==="text"&&u?.type==="text"?(u.raw+=o.raw,u.text+=o.text):n.push(o);continue}if(o=this.tokenizer.emStrong(e,i,d)){e=e.substring(o.raw.length),n.push(o);continue}if(o=this.tokenizer.codespan(e)){e=e.substring(o.raw.length),n.push(o);continue}if(o=this.tokenizer.br(e)){e=e.substring(o.raw.length),n.push(o);continue}if(o=this.tokenizer.del(e,i,d)){e=e.substring(o.raw.length),n.push(o);continue}if(o=this.tokenizer.autolink(e)){e=e.substring(o.raw.length),n.push(o);continue}if(!this.state.inLink&&(o=this.tokenizer.url(e))){e=e.substring(o.raw.length),n.push(o);continue}let p=e;if(this.options.extensions?.startInline){let u=1/0,g=e.slice(1),m;this.options.extensions.startInline.forEach(y=>{m=y.call({lexer:this},g),typeof m=="number"&&m>=0&&(u=Math.min(u,m))}),u<1/0&&u>=0&&(p=e.substring(0,u+1))}if(o=this.tokenizer.inlineText(p)){e=e.substring(o.raw.length),o.raw.slice(-1)!=="_"&&(d=o.raw.slice(-1)),r=!0;let u=n.at(-1);u?.type==="text"?(u.raw+=o.raw,u.text+=o.text):n.push(o);continue}if(e){let u="Infinite loop on byte: "+e.charCodeAt(0);if(this.options.silent){console.error(u);break}else throw new Error(u)}}return n}},Jn=class{constructor(t){V(this,"options");V(this,"parser");this.options=t||Ct}space(t){return""}code({text:t,lang:e,escaped:n}){let i=(e||"").match(Ee.notSpaceStart)?.[0],s=t.replace(Ee.endingNewline,"")+`
`;return i?'<pre><code class="language-'+qe(i)+'">'+(n?s:qe(s,!0))+`</code></pre>
`:"<pre><code>"+(n?s:qe(s,!0))+`</code></pre>
`}blockquote({tokens:t}){return`<blockquote>
${this.parser.parse(t)}</blockquote>
`}html({text:t}){return t}def(t){return""}heading({tokens:t,depth:e}){return`<h${e}>${this.parser.parseInline(t)}</h${e}>
`}hr(t){return`<hr>
`}list(t){let e=t.ordered,n=t.start,i="";for(let r=0;r<t.items.length;r++){let d=t.items[r];i+=this.listitem(d)}let s=e?"ol":"ul",a=e&&n!==1?' start="'+n+'"':"";return"<"+s+a+`>
`+i+"</"+s+`>
`}listitem(t){return`<li>${this.parser.parse(t.tokens)}</li>
`}checkbox({checked:t}){return"<input "+(t?'checked="" ':"")+'disabled="" type="checkbox"> '}paragraph({tokens:t}){return`<p>${this.parser.parseInline(t)}</p>
`}table(t){let e="",n="";for(let s=0;s<t.header.length;s++)n+=this.tablecell(t.header[s]);e+=this.tablerow({text:n});let i="";for(let s=0;s<t.rows.length;s++){let a=t.rows[s];n="";for(let r=0;r<a.length;r++)n+=this.tablecell(a[r]);i+=this.tablerow({text:n})}return i&&(i=`<tbody>${i}</tbody>`),`<table>
<thead>
`+e+`</thead>
`+i+`</table>
`}tablerow({text:t}){return`<tr>
${t}</tr>
`}tablecell(t){let e=this.parser.parseInline(t.tokens),n=t.header?"th":"td";return(t.align?`<${n} align="${t.align}">`:`<${n}>`)+e+`</${n}>
`}strong({tokens:t}){return`<strong>${this.parser.parseInline(t)}</strong>`}em({tokens:t}){return`<em>${this.parser.parseInline(t)}</em>`}codespan({text:t}){return`<code>${qe(t,!0)}</code>`}br(t){return"<br>"}del({tokens:t}){return`<del>${this.parser.parseInline(t)}</del>`}link({href:t,title:e,tokens:n}){let i=this.parser.parseInline(n),s=Ai(t);if(s===null)return i;t=s;let a='<a href="'+t+'"';return e&&(a+=' title="'+qe(e)+'"'),a+=">"+i+"</a>",a}image({href:t,title:e,text:n,tokens:i}){i&&(n=this.parser.parseInline(i,this.parser.textRenderer));let s=Ai(t);if(s===null)return qe(n);t=s;let a=`<img src="${t}" alt="${qe(n)}"`;return e&&(a+=` title="${qe(e)}"`),a+=">",a}text(t){return"tokens"in t&&t.tokens?this.parser.parseInline(t.tokens):"escaped"in t&&t.escaped?t.text:qe(t.text)}},js=class{strong({text:t}){return t}em({text:t}){return t}codespan({text:t}){return t}del({text:t}){return t}html({text:t}){return t}text({text:t}){return t}link({text:t}){return""+t}image({text:t}){return""+t}br(){return""}checkbox({raw:t}){return t}},He=class Os{constructor(e){V(this,"options");V(this,"renderer");V(this,"textRenderer");this.options=e||Ct,this.options.renderer=this.options.renderer||new Jn,this.renderer=this.options.renderer,this.renderer.options=this.options,this.renderer.parser=this,this.textRenderer=new js}static parse(e,n){return new Os(n).parse(e)}static parseInline(e,n){return new Os(n).parseInline(e)}parse(e){let n="";for(let i=0;i<e.length;i++){let s=e[i];if(this.options.extensions?.renderers?.[s.type]){let r=s,d=this.options.extensions.renderers[r.type].call({parser:this},r);if(d!==!1||!["space","hr","heading","code","table","blockquote","list","html","def","paragraph","text"].includes(r.type)){n+=d||"";continue}}let a=s;switch(a.type){case"space":{n+=this.renderer.space(a);break}case"hr":{n+=this.renderer.hr(a);break}case"heading":{n+=this.renderer.heading(a);break}case"code":{n+=this.renderer.code(a);break}case"table":{n+=this.renderer.table(a);break}case"blockquote":{n+=this.renderer.blockquote(a);break}case"list":{n+=this.renderer.list(a);break}case"checkbox":{n+=this.renderer.checkbox(a);break}case"html":{n+=this.renderer.html(a);break}case"def":{n+=this.renderer.def(a);break}case"paragraph":{n+=this.renderer.paragraph(a);break}case"text":{n+=this.renderer.text(a);break}default:{let r='Token with "'+a.type+'" type was not found.';if(this.options.silent)return console.error(r),"";throw new Error(r)}}}return n}parseInline(e,n=this.renderer){let i="";for(let s=0;s<e.length;s++){let a=e[s];if(this.options.extensions?.renderers?.[a.type]){let d=this.options.extensions.renderers[a.type].call({parser:this},a);if(d!==!1||!["escape","html","link","image","strong","em","codespan","br","del","text"].includes(a.type)){i+=d||"";continue}}let r=a;switch(r.type){case"escape":{i+=n.text(r);break}case"html":{i+=n.html(r);break}case"link":{i+=n.link(r);break}case"image":{i+=n.image(r);break}case"checkbox":{i+=n.checkbox(r);break}case"strong":{i+=n.strong(r);break}case"em":{i+=n.em(r);break}case"codespan":{i+=n.codespan(r);break}case"br":{i+=n.br(r);break}case"del":{i+=n.del(r);break}case"text":{i+=n.text(r);break}default:{let d='Token with "'+r.type+'" type was not found.';if(this.options.silent)return console.error(d),"";throw new Error(d)}}}return i}},jn,gn=(jn=class{constructor(t){V(this,"options");V(this,"block");this.options=t||Ct}preprocess(t){return t}postprocess(t){return t}processAllTokens(t){return t}emStrongMask(t){return t}provideLexer(){return this.block?Be.lex:Be.lexInline}provideParser(){return this.block?He.parse:He.parseInline}},V(jn,"passThroughHooks",new Set(["preprocess","postprocess","processAllTokens","emStrongMask"])),V(jn,"passThroughHooksRespectAsync",new Set(["preprocess","postprocess","processAllTokens"])),jn),jo=class{constructor(...t){V(this,"defaults",Ds());V(this,"options",this.setOptions);V(this,"parse",this.parseMarkdown(!0));V(this,"parseInline",this.parseMarkdown(!1));V(this,"Parser",He);V(this,"Renderer",Jn);V(this,"TextRenderer",js);V(this,"Lexer",Be);V(this,"Tokenizer",Qn);V(this,"Hooks",gn);this.use(...t)}walkTokens(t,e){let n=[];for(let i of t)switch(n=n.concat(e.call(this,i)),i.type){case"table":{let s=i;for(let a of s.header)n=n.concat(this.walkTokens(a.tokens,e));for(let a of s.rows)for(let r of a)n=n.concat(this.walkTokens(r.tokens,e));break}case"list":{let s=i;n=n.concat(this.walkTokens(s.items,e));break}default:{let s=i;this.defaults.extensions?.childTokens?.[s.type]?this.defaults.extensions.childTokens[s.type].forEach(a=>{let r=s[a].flat(1/0);n=n.concat(this.walkTokens(r,e))}):s.tokens&&(n=n.concat(this.walkTokens(s.tokens,e)))}}return n}use(...t){let e=this.defaults.extensions||{renderers:{},childTokens:{}};return t.forEach(n=>{let i={...n};if(i.async=this.defaults.async||i.async||!1,n.extensions&&(n.extensions.forEach(s=>{if(!s.name)throw new Error("extension name required");if("renderer"in s){let a=e.renderers[s.name];a?e.renderers[s.name]=function(...r){let d=s.renderer.apply(this,r);return d===!1&&(d=a.apply(this,r)),d}:e.renderers[s.name]=s.renderer}if("tokenizer"in s){if(!s.level||s.level!=="block"&&s.level!=="inline")throw new Error("extension level must be 'block' or 'inline'");let a=e[s.level];a?a.unshift(s.tokenizer):e[s.level]=[s.tokenizer],s.start&&(s.level==="block"?e.startBlock?e.startBlock.push(s.start):e.startBlock=[s.start]:s.level==="inline"&&(e.startInline?e.startInline.push(s.start):e.startInline=[s.start]))}"childTokens"in s&&s.childTokens&&(e.childTokens[s.name]=s.childTokens)}),i.extensions=e),n.renderer){let s=this.defaults.renderer||new Jn(this.defaults);for(let a in n.renderer){if(!(a in s))throw new Error(`renderer '${a}' does not exist`);if(["options","parser"].includes(a))continue;let r=a,d=n.renderer[r],o=s[r];s[r]=(...p)=>{let u=d.apply(s,p);return u===!1&&(u=o.apply(s,p)),u||""}}i.renderer=s}if(n.tokenizer){let s=this.defaults.tokenizer||new Qn(this.defaults);for(let a in n.tokenizer){if(!(a in s))throw new Error(`tokenizer '${a}' does not exist`);if(["options","rules","lexer"].includes(a))continue;let r=a,d=n.tokenizer[r],o=s[r];s[r]=(...p)=>{let u=d.apply(s,p);return u===!1&&(u=o.apply(s,p)),u}}i.tokenizer=s}if(n.hooks){let s=this.defaults.hooks||new gn;for(let a in n.hooks){if(!(a in s))throw new Error(`hook '${a}' does not exist`);if(["options","block"].includes(a))continue;let r=a,d=n.hooks[r],o=s[r];gn.passThroughHooks.has(a)?s[r]=p=>{if(this.defaults.async&&gn.passThroughHooksRespectAsync.has(a))return(async()=>{let g=await d.call(s,p);return o.call(s,g)})();let u=d.call(s,p);return o.call(s,u)}:s[r]=(...p)=>{if(this.defaults.async)return(async()=>{let g=await d.apply(s,p);return g===!1&&(g=await o.apply(s,p)),g})();let u=d.apply(s,p);return u===!1&&(u=o.apply(s,p)),u}}i.hooks=s}if(n.walkTokens){let s=this.defaults.walkTokens,a=n.walkTokens;i.walkTokens=function(r){let d=[];return d.push(a.call(this,r)),s&&(d=d.concat(s.call(this,r))),d}}this.defaults={...this.defaults,...i}}),this}setOptions(t){return this.defaults={...this.defaults,...t},this}lexer(t,e){return Be.lex(t,e??this.defaults)}parser(t,e){return He.parse(t,e??this.defaults)}parseMarkdown(t){return(e,n)=>{let i={...n},s={...this.defaults,...i},a=this.onError(!!s.silent,!!s.async);if(this.defaults.async===!0&&i.async===!1)return a(new Error("marked(): The async option was set to true by an extension. Remove async: false from the parse options object to return a Promise."));if(typeof e>"u"||e===null)return a(new Error("marked(): input parameter is undefined or null"));if(typeof e!="string")return a(new Error("marked(): input parameter is of type "+Object.prototype.toString.call(e)+", string expected"));if(s.hooks&&(s.hooks.options=s,s.hooks.block=t),s.async)return(async()=>{let r=s.hooks?await s.hooks.preprocess(e):e,d=await(s.hooks?await s.hooks.provideLexer():t?Be.lex:Be.lexInline)(r,s),o=s.hooks?await s.hooks.processAllTokens(d):d;s.walkTokens&&await Promise.all(this.walkTokens(o,s.walkTokens));let p=await(s.hooks?await s.hooks.provideParser():t?He.parse:He.parseInline)(o,s);return s.hooks?await s.hooks.postprocess(p):p})().catch(a);try{s.hooks&&(e=s.hooks.preprocess(e));let r=(s.hooks?s.hooks.provideLexer():t?Be.lex:Be.lexInline)(e,s);s.hooks&&(r=s.hooks.processAllTokens(r)),s.walkTokens&&this.walkTokens(r,s.walkTokens);let d=(s.hooks?s.hooks.provideParser():t?He.parse:He.parseInline)(r,s);return s.hooks&&(d=s.hooks.postprocess(d)),d}catch(r){return a(r)}}}onError(t,e){return n=>{if(n.message+=`
Please report this to https://github.com/markedjs/marked.`,t){let i="<p>An error occurred:</p><pre>"+qe(n.message+"",!0)+"</pre>";return e?Promise.resolve(i):i}if(e)return Promise.reject(n);throw n}}},Ot=new jo;function F(t,e){return Ot.parse(t,e)}F.options=F.setOptions=function(t){return Ot.setOptions(t),F.defaults=Ot.defaults,Ji(F.defaults),F};F.getDefaults=Ds;F.defaults=Ct;F.use=function(...t){return Ot.use(...t),F.defaults=Ot.defaults,Ji(F.defaults),F};F.walkTokens=function(t,e){return Ot.walkTokens(t,e)};F.parseInline=Ot.parseInline;F.Parser=He;F.parser=He.parse;F.Renderer=Jn;F.TextRenderer=js;F.Lexer=Be;F.lexer=Be.lex;F.Tokenizer=Qn;F.Hooks=gn;F.parse=F;F.options;F.setOptions;F.use;F.walkTokens;F.parseInline;He.parse;Be.lex;function qo(t){return t&&t.__esModule&&Object.prototype.hasOwnProperty.call(t,"default")?t.default:t}var ks,Oi;function Ko(){if(Oi)return ks;Oi=1;function t(l){return l instanceof Map?l.clear=l.delete=l.set=function(){throw new Error("map is read-only")}:l instanceof Set&&(l.add=l.clear=l.delete=function(){throw new Error("set is read-only")}),Object.freeze(l),Object.getOwnPropertyNames(l).forEach(h=>{const b=l[h],O=typeof b;(O==="object"||O==="function")&&!Object.isFrozen(b)&&t(b)}),l}class e{constructor(h){h.data===void 0&&(h.data={}),this.data=h.data,this.isMatchIgnored=!1}ignoreMatch(){this.isMatchIgnored=!0}}function n(l){return l.replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;").replace(/'/g,"&#x27;")}function i(l,...h){const b=Object.create(null);for(const O in l)b[O]=l[O];return h.forEach(function(O){for(const se in O)b[se]=O[se]}),b}const s="</span>",a=l=>!!l.scope,r=(l,{prefix:h})=>{if(l.startsWith("language:"))return l.replace("language:","language-");if(l.includes(".")){const b=l.split(".");return[`${h}${b.shift()}`,...b.map((O,se)=>`${O}${"_".repeat(se+1)}`)].join(" ")}return`${h}${l}`};class d{constructor(h,b){this.buffer="",this.classPrefix=b.classPrefix,h.walk(this)}addText(h){this.buffer+=n(h)}openNode(h){if(!a(h))return;const b=r(h.scope,{prefix:this.classPrefix});this.span(b)}closeNode(h){a(h)&&(this.buffer+=s)}value(){return this.buffer}span(h){this.buffer+=`<span class="${h}">`}}const o=(l={})=>{const h={children:[]};return Object.assign(h,l),h};class p{constructor(){this.rootNode=o(),this.stack=[this.rootNode]}get top(){return this.stack[this.stack.length-1]}get root(){return this.rootNode}add(h){this.top.children.push(h)}openNode(h){const b=o({scope:h});this.add(b),this.stack.push(b)}closeNode(){if(this.stack.length>1)return this.stack.pop()}closeAllNodes(){for(;this.closeNode(););}toJSON(){return JSON.stringify(this.rootNode,null,4)}walk(h){return this.constructor._walk(h,this.rootNode)}static _walk(h,b){return typeof b=="string"?h.addText(b):b.children&&(h.openNode(b),b.children.forEach(O=>this._walk(h,O)),h.closeNode(b)),h}static _collapse(h){typeof h!="string"&&h.children&&(h.children.every(b=>typeof b=="string")?h.children=[h.children.join("")]:h.children.forEach(b=>{p._collapse(b)}))}}class u extends p{constructor(h){super(),this.options=h}addText(h){h!==""&&this.add(h)}startScope(h){this.openNode(h)}endScope(){this.closeNode()}__addSublanguage(h,b){const O=h.root;b&&(O.scope=`language:${b}`),this.add(O)}toHTML(){return new d(this,this.options).value()}finalize(){return this.closeAllNodes(),!0}}function g(l){return l?typeof l=="string"?l:l.source:null}function m(l){return R("(?=",l,")")}function y(l){return R("(?:",l,")*")}function E(l){return R("(?:",l,")?")}function R(...l){return l.map(b=>g(b)).join("")}function B(l){const h=l[l.length-1];return typeof h=="object"&&h.constructor===Object?(l.splice(l.length-1,1),h):{}}function j(...l){return"("+(B(l).capture?"":"?:")+l.map(O=>g(O)).join("|")+")"}function P(l){return new RegExp(l.toString()+"|").exec("").length-1}function D(l,h){const b=l&&l.exec(h);return b&&b.index===0}const z=/\[(?:[^\\\]]|\\.)*\]|\(\??|\\([1-9][0-9]*)|\\./;function Y(l,{joinWith:h}){let b=0;return l.map(O=>{b+=1;const se=b;let ee=g(O),k="";for(;ee.length>0;){const v=z.exec(ee);if(!v){k+=ee;break}k+=ee.substring(0,v.index),ee=ee.substring(v.index+v[0].length),v[0][0]==="\\"&&v[1]?k+="\\"+String(Number(v[1])+se):(k+=v[0],v[0]==="("&&b++)}return k}).map(O=>`(${O})`).join(h)}const te=/\b\B/,tt="[a-zA-Z]\\w*",Le="[a-zA-Z_]\\w*",nt="\\b\\d+(\\.\\d+)?",st="(-?)(\\b0[xX][a-fA-F0-9]+|(\\b\\d+(\\.\\d*)?|\\.\\d+)([eE][-+]?\\d+)?)",ie="\\b(0b[01]+)",Ve="!|!=|!==|%|%=|&|&&|&=|\\*|\\*=|\\+|\\+=|,|-|-=|/=|/|:|;|<<|<<=|<=|<|===|==|=|>>>=|>>=|>=|>>>|>>|>|\\?|\\[|\\{|\\(|\\^|\\^=|\\||\\|=|\\|\\||~",Xe=(l={})=>{const h=/^#![ ]*\//;return l.binary&&(l.begin=R(h,/.*\b/,l.binary,/\b.*/)),i({scope:"meta",begin:h,end:/$/,relevance:0,"on:begin":(b,O)=>{b.index!==0&&O.ignoreMatch()}},l)},Ae={begin:"\\\\[\\s\\S]",relevance:0},bt={scope:"string",begin:"'",end:"'",illegal:"\\n",contains:[Ae]},Ye={scope:"string",begin:'"',end:'"',illegal:"\\n",contains:[Ae]},mt={begin:/\b(a|an|the|are|I'm|isn't|don't|doesn't|won't|but|just|should|pretty|simply|enough|gonna|going|wtf|so|such|will|you|your|they|like|more)\b/},H=function(l,h,b={}){const O=i({scope:"comment",begin:l,end:h,contains:[]},b);O.contains.push({scope:"doctag",begin:"[ ]*(?=(TODO|FIXME|NOTE|BUG|OPTIMIZE|HACK|XXX):)",end:/(TODO|FIXME|NOTE|BUG|OPTIMIZE|HACK|XXX):/,excludeBegin:!0,relevance:0});const se=j("I","a","is","so","us","to","at","if","in","it","on",/[A-Za-z]+['](d|ve|re|ll|t|s|n)/,/[A-Za-z]+[-][a-z]+/,/[A-Za-z][a-z]{2,}/);return O.contains.push({begin:R(/[ ]+/,"(",se,/[.]?[:]?([.][ ]|[ ])/,"){3}")}),O},he=H("//","$"),be=H("/\\*","\\*/"),q=H("#","$"),Ne={scope:"number",begin:nt,relevance:0},J={scope:"number",begin:st,relevance:0},Sn={scope:"number",begin:ie,relevance:0},ne={scope:"regexp",begin:/\/(?=[^/\n]*\/)/,end:/\/[gimuy]*/,contains:[Ae,{begin:/\[/,end:/\]/,relevance:0,contains:[Ae]}]},_t={scope:"title",begin:tt,relevance:0},It={scope:"title",begin:Le,relevance:0},Fe={begin:"\\.\\s*"+Le,relevance:0};var it=Object.freeze({__proto__:null,APOS_STRING_MODE:bt,BACKSLASH_ESCAPE:Ae,BINARY_NUMBER_MODE:Sn,BINARY_NUMBER_RE:ie,COMMENT:H,C_BLOCK_COMMENT_MODE:be,C_LINE_COMMENT_MODE:he,C_NUMBER_MODE:J,C_NUMBER_RE:st,END_SAME_AS_BEGIN:function(l){return Object.assign(l,{"on:begin":(h,b)=>{b.data._beginMatch=h[1]},"on:end":(h,b)=>{b.data._beginMatch!==h[1]&&b.ignoreMatch()}})},HASH_COMMENT_MODE:q,IDENT_RE:tt,MATCH_NOTHING_RE:te,METHOD_GUARD:Fe,NUMBER_MODE:Ne,NUMBER_RE:nt,PHRASAL_WORDS_MODE:mt,QUOTE_STRING_MODE:Ye,REGEXP_MODE:ne,RE_STARTERS_RE:Ve,SHEBANG:Xe,TITLE_MODE:_t,UNDERSCORE_IDENT_RE:Le,UNDERSCORE_TITLE_MODE:It});function Tn(l,h){l.input[l.index-1]==="."&&h.ignoreMatch()}function An(l,h){l.className!==void 0&&(l.scope=l.className,delete l.className)}function rt(l,h){h&&l.beginKeywords&&(l.begin="\\b("+l.beginKeywords.split(" ").join("|")+")(?!\\.)(?=\\b|\\s)",l.__beforeBegin=Tn,l.keywords=l.keywords||l.beginKeywords,delete l.beginKeywords,l.relevance===void 0&&(l.relevance=0))}function Mt(l,h){Array.isArray(l.illegal)&&(l.illegal=j(...l.illegal))}function Qe(l,h){if(l.match){if(l.begin||l.end)throw new Error("begin & end are not supported with match");l.begin=l.match,delete l.match}}function Yt(l,h){l.relevance===void 0&&(l.relevance=1)}const Qt=(l,h)=>{if(!l.beforeMatch)return;if(l.starts)throw new Error("beforeMatch cannot be used with starts");const b=Object.assign({},l);Object.keys(l).forEach(O=>{delete l[O]}),l.keywords=b.keywords,l.begin=R(b.beforeMatch,m(b.begin)),l.starts={relevance:0,contains:[Object.assign(b,{endsParent:!0})]},l.relevance=0,delete b.beforeMatch},at=["of","and","for","in","not","or","if","then","parent","list","value"],Lt="keyword";function xt(l,h,b=Lt){const O=Object.create(null);return typeof l=="string"?se(b,l.split(" ")):Array.isArray(l)?se(b,l):Object.keys(l).forEach(function(ee){Object.assign(O,xt(l[ee],h,ee))}),O;function se(ee,k){h&&(k=k.map(v=>v.toLowerCase())),k.forEach(function(v){const C=v.split("|");O[C[0]]=[ee,$n(C[0],C[1])]})}}function $n(l,h){return h?Number(h):Rn(l)?0:1}function Rn(l){return at.includes(l.toLowerCase())}const On={},Pe=l=>{console.error(l)},ot=(l,...h)=>{console.log(`WARN: ${l}`,...h)},$e=(l,h)=>{On[`${l}/${h}`]||(console.log(`Deprecated as of ${l}. ${h}`),On[`${l}/${h}`]=!0)},xe=new Error;function Pt(l,h,{key:b}){let O=0;const se=l[b],ee={},k={};for(let v=1;v<=h.length;v++)k[v+O]=se[v],ee[v+O]=!0,O+=P(h[v-1]);l[b]=k,l[b]._emit=ee,l[b]._multi=!0}function Cn(l){if(Array.isArray(l.begin)){if(l.skip||l.excludeBegin||l.returnBegin)throw Pe("skip, excludeBegin, returnBegin not compatible with beginScope: {}"),xe;if(typeof l.beginScope!="object"||l.beginScope===null)throw Pe("beginScope must be object"),xe;Pt(l,l.begin,{key:"beginScope"}),l.begin=Y(l.begin,{joinWith:""})}}function Nn(l){if(Array.isArray(l.end)){if(l.skip||l.excludeEnd||l.returnEnd)throw Pe("skip, excludeEnd, returnEnd not compatible with endScope: {}"),xe;if(typeof l.endScope!="object"||l.endScope===null)throw Pe("endScope must be object"),xe;Pt(l,l.end,{key:"endScope"}),l.end=Y(l.end,{joinWith:""})}}function Jt(l){l.scope&&typeof l.scope=="object"&&l.scope!==null&&(l.beginScope=l.scope,delete l.scope)}function In(l){Jt(l),typeof l.beginScope=="string"&&(l.beginScope={_wrap:l.beginScope}),typeof l.endScope=="string"&&(l.endScope={_wrap:l.endScope}),Cn(l),Nn(l)}function Dt(l){function h(k,v){return new RegExp(g(k),"m"+(l.case_insensitive?"i":"")+(l.unicodeRegex?"u":"")+(v?"g":""))}class b{constructor(){this.matchIndexes={},this.regexes=[],this.matchAt=1,this.position=0}addRule(v,C){C.position=this.position++,this.matchIndexes[this.matchAt]=C,this.regexes.push([C,v]),this.matchAt+=P(v)+1}compile(){this.regexes.length===0&&(this.exec=()=>null);const v=this.regexes.map(C=>C[1]);this.matcherRe=h(Y(v,{joinWith:"|"}),!0),this.lastIndex=0}exec(v){this.matcherRe.lastIndex=this.lastIndex;const C=this.matcherRe.exec(v);if(!C)return null;const ae=C.findIndex((Re,Ut)=>Ut>0&&Re!==void 0),G=this.matchIndexes[ae];return C.splice(0,ae),Object.assign(C,G)}}class O{constructor(){this.rules=[],this.multiRegexes=[],this.count=0,this.lastIndex=0,this.regexIndex=0}getMatcher(v){if(this.multiRegexes[v])return this.multiRegexes[v];const C=new b;return this.rules.slice(v).forEach(([ae,G])=>C.addRule(ae,G)),C.compile(),this.multiRegexes[v]=C,C}resumingScanAtSamePosition(){return this.regexIndex!==0}considerAll(){this.regexIndex=0}addRule(v,C){this.rules.push([v,C]),C.type==="begin"&&this.count++}exec(v){const C=this.getMatcher(this.regexIndex);C.lastIndex=this.lastIndex;let ae=C.exec(v);if(this.resumingScanAtSamePosition()&&!(ae&&ae.index===this.lastIndex)){const G=this.getMatcher(0);G.lastIndex=this.lastIndex+1,ae=G.exec(v)}return ae&&(this.regexIndex+=ae.position+1,this.regexIndex===this.count&&this.considerAll()),ae}}function se(k){const v=new O;return k.contains.forEach(C=>v.addRule(C.begin,{rule:C,type:"begin"})),k.terminatorEnd&&v.addRule(k.terminatorEnd,{type:"end"}),k.illegal&&v.addRule(k.illegal,{type:"illegal"}),v}function ee(k,v){const C=k;if(k.isCompiled)return C;[An,Qe,In,Qt].forEach(G=>G(k,v)),l.compilerExtensions.forEach(G=>G(k,v)),k.__beforeBegin=null,[rt,Mt,Yt].forEach(G=>G(k,v)),k.isCompiled=!0;let ae=null;return typeof k.keywords=="object"&&k.keywords.$pattern&&(k.keywords=Object.assign({},k.keywords),ae=k.keywords.$pattern,delete k.keywords.$pattern),ae=ae||/\w+/,k.keywords&&(k.keywords=xt(k.keywords,l.case_insensitive)),C.keywordPatternRe=h(ae,!0),v&&(k.begin||(k.begin=/\B|\b/),C.beginRe=h(C.begin),!k.end&&!k.endsWithParent&&(k.end=/\B|\b/),k.end&&(C.endRe=h(C.end)),C.terminatorEnd=g(C.end)||"",k.endsWithParent&&v.terminatorEnd&&(C.terminatorEnd+=(k.end?"|":"")+v.terminatorEnd)),k.illegal&&(C.illegalRe=h(k.illegal)),k.contains||(k.contains=[]),k.contains=[].concat(...k.contains.map(function(G){return De(G==="self"?k:G)})),k.contains.forEach(function(G){ee(G,C)}),k.starts&&ee(k.starts,v),C.matcher=se(C),C}if(l.compilerExtensions||(l.compilerExtensions=[]),l.contains&&l.contains.includes("self"))throw new Error("ERR: contains `self` is not supported at the top-level of a language.  See documentation.");return l.classNameAliases=i(l.classNameAliases||{}),ee(l)}function yt(l){return l?l.endsWithParent||yt(l.starts):!1}function De(l){return l.variants&&!l.cachedVariants&&(l.cachedVariants=l.variants.map(function(h){return i(l,{variants:null},h)})),l.cachedVariants?l.cachedVariants:yt(l)?i(l,{starts:l.starts?i(l.starts):null}):Object.isFrozen(l)?i(l):l}var lt="11.11.1";class en extends Error{constructor(h,b){super(h),this.name="HTMLInjectionError",this.html=b}}const wt=n,Mn=i,Et=Symbol("nomatch"),zt=7,Ln=function(l){const h=Object.create(null),b=Object.create(null),O=[];let se=!0;const ee="Could not find the language '{}', did you forget to load/include a language module?",k={disableAutodetect:!0,name:"Plain text",contains:[]};let v={ignoreUnescapedHTML:!1,throwUnescapedHTML:!1,noHighlightRe:/^(no-?highlight)$/i,languageDetectRe:/\blang(?:uage)?-([\w-]+)\b/i,classPrefix:"hljs-",cssSelector:"pre code",languages:null,__emitter:u};function C(_){return v.noHighlightRe.test(_)}function ae(_){let w=_.className+" ";w+=_.parentNode?_.parentNode.className:"";const M=v.languageDetectRe.exec(w);if(M){const K=f(M[1]);return K||(ot(ee.replace("{}",M[1])),ot("Falling back to no-highlight mode for this block.",_)),K?M[1]:"no-highlight"}return w.split(/\s+/).find(K=>C(K)||f(K))}function G(_,w,M){let K="",ce="";typeof w=="object"?(K=_,M=w.ignoreIllegals,ce=w.language):($e("10.7.0","highlight(lang, code, ...args) has been deprecated."),$e("10.7.0",`Please use highlight(code, options) instead.
https://github.com/highlightjs/highlight.js/issues/2277`),ce=_,K=w),M===void 0&&(M=!0);const Ue={code:K,language:ce};Se("before:highlight",Ue);const dt=Ue.result?Ue.result:Re(Ue.language,Ue.code,M);return dt.code=Ue.code,Se("after:highlight",dt),dt}function Re(_,w,M,K){const ce=Object.create(null);function Ue(x,$){return x.keywords[$]}function dt(){if(!N.keywords){ge.addText(Q);return}let x=0;N.keywordPatternRe.lastIndex=0;let $=N.keywordPatternRe.exec(Q),I="";for(;$;){I+=Q.substring(x,$.index);const Z=We.case_insensitive?$[0].toLowerCase():$[0],_e=Ue(N,Z);if(_e){const[Je,Mr]=_e;if(ge.addText(I),I="",ce[Z]=(ce[Z]||0)+1,ce[Z]<=zt&&(Bn+=Mr),Je.startsWith("_"))I+=$[0];else{const Lr=We.classNameAliases[Je]||Je;Ge($[0],Lr)}}else I+=$[0];x=N.keywordPatternRe.lastIndex,$=N.keywordPatternRe.exec(Q)}I+=Q.substring(x),ge.addText(I)}function zn(){if(Q==="")return;let x=null;if(typeof N.subLanguage=="string"){if(!h[N.subLanguage]){ge.addText(Q);return}x=Re(N.subLanguage,Q,!0,Qs[N.subLanguage]),Qs[N.subLanguage]=x._top}else x=Bt(Q,N.subLanguage.length?N.subLanguage:null);N.relevance>0&&(Bn+=x.relevance),ge.__addSublanguage(x._emitter,x.language)}function Oe(){N.subLanguage!=null?zn():dt(),Q=""}function Ge(x,$){x!==""&&(ge.startScope($),ge.addText(x),ge.endScope())}function Zs(x,$){let I=1;const Z=$.length-1;for(;I<=Z;){if(!x._emit[I]){I++;continue}const _e=We.classNameAliases[x[I]]||x[I],Je=$[I];_e?Ge(Je,_e):(Q=Je,dt(),Q=""),I++}}function Vs(x,$){return x.scope&&typeof x.scope=="string"&&ge.openNode(We.classNameAliases[x.scope]||x.scope),x.beginScope&&(x.beginScope._wrap?(Ge(Q,We.classNameAliases[x.beginScope._wrap]||x.beginScope._wrap),Q=""):x.beginScope._multi&&(Zs(x.beginScope,$),Q="")),N=Object.create(x,{parent:{value:N}}),N}function Xs(x,$,I){let Z=D(x.endRe,I);if(Z){if(x["on:end"]){const _e=new e(x);x["on:end"]($,_e),_e.isMatchIgnored&&(Z=!1)}if(Z){for(;x.endsParent&&x.parent;)x=x.parent;return x}}if(x.endsWithParent)return Xs(x.parent,$,I)}function Rr(x){return N.matcher.regexIndex===0?(Q+=x[0],1):(fs=!0,0)}function Or(x){const $=x[0],I=x.rule,Z=new e(I),_e=[I.__beforeBegin,I["on:begin"]];for(const Je of _e)if(Je&&(Je(x,Z),Z.isMatchIgnored))return Rr($);return I.skip?Q+=$:(I.excludeBegin&&(Q+=$),Oe(),!I.returnBegin&&!I.excludeBegin&&(Q=$)),Vs(I,x),I.returnBegin?0:$.length}function Cr(x){const $=x[0],I=w.substring(x.index),Z=Xs(N,x,I);if(!Z)return Et;const _e=N;N.endScope&&N.endScope._wrap?(Oe(),Ge($,N.endScope._wrap)):N.endScope&&N.endScope._multi?(Oe(),Zs(N.endScope,x)):_e.skip?Q+=$:(_e.returnEnd||_e.excludeEnd||(Q+=$),Oe(),_e.excludeEnd&&(Q=$));do N.scope&&ge.closeNode(),!N.skip&&!N.subLanguage&&(Bn+=N.relevance),N=N.parent;while(N!==Z.parent);return Z.starts&&Vs(Z.starts,x),_e.returnEnd?0:$.length}function Nr(){const x=[];for(let $=N;$!==We;$=$.parent)$.scope&&x.unshift($.scope);x.forEach($=>ge.openNode($))}let Un={};function Ys(x,$){const I=$&&$[0];if(Q+=x,I==null)return Oe(),0;if(Un.type==="begin"&&$.type==="end"&&Un.index===$.index&&I===""){if(Q+=w.slice($.index,$.index+1),!se){const Z=new Error(`0 width match regex (${_})`);throw Z.languageName=_,Z.badRule=Un.rule,Z}return 1}if(Un=$,$.type==="begin")return Or($);if($.type==="illegal"&&!M){const Z=new Error('Illegal lexeme "'+I+'" for mode "'+(N.scope||"<unnamed>")+'"');throw Z.mode=N,Z}else if($.type==="end"){const Z=Cr($);if(Z!==Et)return Z}if($.type==="illegal"&&I==="")return Q+=`
`,1;if(gs>1e5&&gs>$.index*3)throw new Error("potential infinite loop, way more iterations than matches");return Q+=I,I.length}const We=f(_);if(!We)throw Pe(ee.replace("{}",_)),new Error('Unknown language: "'+_+'"');const Ir=Dt(We);let hs="",N=K||Ir;const Qs={},ge=new v.__emitter(v);Nr();let Q="",Bn=0,kt=0,gs=0,fs=!1;try{if(We.__emitTokens)We.__emitTokens(w,ge);else{for(N.matcher.considerAll();;){gs++,fs?fs=!1:N.matcher.considerAll(),N.matcher.lastIndex=kt;const x=N.matcher.exec(w);if(!x)break;const $=w.substring(kt,x.index),I=Ys($,x);kt=x.index+I}Ys(w.substring(kt))}return ge.finalize(),hs=ge.toHTML(),{language:_,value:hs,relevance:Bn,illegal:!1,_emitter:ge,_top:N}}catch(x){if(x.message&&x.message.includes("Illegal"))return{language:_,value:wt(w),illegal:!0,relevance:0,_illegalBy:{message:x.message,index:kt,context:w.slice(kt-100,kt+100),mode:x.mode,resultSoFar:hs},_emitter:ge};if(se)return{language:_,value:wt(w),illegal:!1,relevance:0,errorRaised:x,_emitter:ge,_top:N};throw x}}function Ut(_){const w={value:wt(_),illegal:!1,relevance:0,_top:k,_emitter:new v.__emitter(v)};return w._emitter.addText(_),w}function Bt(_,w){w=w||v.languages||Object.keys(h);const M=Ut(_),K=w.filter(f).filter(X).map(Oe=>Re(Oe,_,!1));K.unshift(M);const ce=K.sort((Oe,Ge)=>{if(Oe.relevance!==Ge.relevance)return Ge.relevance-Oe.relevance;if(Oe.language&&Ge.language){if(f(Oe.language).supersetOf===Ge.language)return 1;if(f(Ge.language).supersetOf===Oe.language)return-1}return 0}),[Ue,dt]=ce,zn=Ue;return zn.secondBest=dt,zn}function tn(_,w,M){const K=w&&b[w]||M;_.classList.add("hljs"),_.classList.add(`language-${K}`)}function Ht(_){let w=null;const M=ae(_);if(C(M))return;if(Se("before:highlightElement",{el:_,language:M}),_.dataset.highlighted){console.log("Element previously highlighted. To highlight again, first unset `dataset.highlighted`.",_);return}if(_.children.length>0&&(v.ignoreUnescapedHTML||(console.warn("One of your code blocks includes unescaped HTML. This is a potentially serious security risk."),console.warn("https://github.com/highlightjs/highlight.js/wiki/security"),console.warn("The element with unescaped HTML:"),console.warn(_)),v.throwUnescapedHTML))throw new en("One of your code blocks includes unescaped HTML.",_.innerHTML);w=_;const K=w.textContent,ce=M?G(K,{language:M,ignoreIllegals:!0}):Bt(K);_.innerHTML=ce.value,_.dataset.highlighted="yes",tn(_,M,ce.language),_.result={language:ce.language,re:ce.relevance,relevance:ce.relevance},ce.secondBest&&(_.secondBest={language:ce.secondBest.language,relevance:ce.secondBest.relevance}),Se("after:highlightElement",{el:_,result:ce,text:K})}function ze(_){v=Mn(v,_)}const Pn=()=>{vt(),$e("10.6.0","initHighlighting() deprecated.  Use highlightAll() now.")};function Dn(){vt(),$e("10.6.0","initHighlightingOnLoad() deprecated.  Use highlightAll() now.")}let nn=!1;function vt(){function _(){vt()}if(document.readyState==="loading"){nn||window.addEventListener("DOMContentLoaded",_,!1),nn=!0;return}document.querySelectorAll(v.cssSelector).forEach(Ht)}function us(_,w){let M=null;try{M=w(l)}catch(K){if(Pe("Language definition for '{}' could not be registered.".replace("{}",_)),se)Pe(K);else throw K;M=k}M.name||(M.name=_),h[_]=M,M.rawDefinition=w.bind(null,l),M.aliases&&S(M.aliases,{languageName:_})}function A(_){delete h[_];for(const w of Object.keys(b))b[w]===_&&delete b[w]}function c(){return Object.keys(h)}function f(_){return _=(_||"").toLowerCase(),h[_]||h[b[_]]}function S(_,{languageName:w}){typeof _=="string"&&(_=[_]),_.forEach(M=>{b[M.toLowerCase()]=w})}function X(_){const w=f(_);return w&&!w.disableAutodetect}function pe(_){_["before:highlightBlock"]&&!_["before:highlightElement"]&&(_["before:highlightElement"]=w=>{_["before:highlightBlock"](Object.assign({block:w.el},w))}),_["after:highlightBlock"]&&!_["after:highlightElement"]&&(_["after:highlightElement"]=w=>{_["after:highlightBlock"](Object.assign({block:w.el},w))})}function re(_){pe(_),O.push(_)}function me(_){const w=O.indexOf(_);w!==-1&&O.splice(w,1)}function Se(_,w){const M=_;O.forEach(function(K){K[M]&&K[M](w)})}function ct(_){return $e("10.7.0","highlightBlock will be removed entirely in v12.0"),$e("10.7.0","Please use highlightElement now."),Ht(_)}Object.assign(l,{highlight:G,highlightAuto:Bt,highlightAll:vt,highlightElement:Ht,highlightBlock:ct,configure:ze,initHighlighting:Pn,initHighlightingOnLoad:Dn,registerLanguage:us,unregisterLanguage:A,listLanguages:c,getLanguage:f,registerAliases:S,autoDetection:X,inherit:Mn,addPlugin:re,removePlugin:me}),l.debugMode=function(){se=!1},l.safeMode=function(){se=!0},l.versionString=lt,l.regex={concat:R,lookahead:m,either:j,optional:E,anyNumberOfTimes:y};for(const _ in it)typeof it[_]=="object"&&t(it[_]);return Object.assign(l,it),l},Ie=Ln({});return Ie.newInstance=()=>Ln({}),ks=Ie,Ie.HighlightJS=Ie,Ie.default=Ie,ks}var Zo=Ko();const de=qo(Zo);function qs(t){const e=t.regex,n={},i={begin:/\$\{/,end:/\}/,contains:["self",{begin:/:-/,contains:[n]}]};Object.assign(n,{className:"variable",variants:[{begin:e.concat(/\$[\w\d#@][\w\d_]*/,"(?![\\w\\d])(?![$])")},i]});const s={className:"subst",begin:/\$\(/,end:/\)/,contains:[t.BACKSLASH_ESCAPE]},a=t.inherit(t.COMMENT(),{match:[/(^|\s)/,/#.*$/],scope:{2:"comment"}}),r={begin:/<<-?\s*(?=\w+)/,starts:{contains:[t.END_SAME_AS_BEGIN({begin:/(\w+)/,end:/(\w+)/,className:"string"})]}},d={className:"string",begin:/"/,end:/"/,contains:[t.BACKSLASH_ESCAPE,n,s]};s.contains.push(d);const o={match:/\\"/},p={className:"string",begin:/'/,end:/'/},u={match:/\\'/},g={begin:/\$?\(\(/,end:/\)\)/,contains:[{begin:/\d+#[0-9a-f]+/,className:"number"},t.NUMBER_MODE,n]},m=["fish","bash","zsh","sh","csh","ksh","tcsh","dash","scsh"],y=t.SHEBANG({binary:`(${m.join("|")})`,relevance:10}),E={className:"function",begin:/\w[\w\d_]*\s*\(\s*\)\s*\{/,returnBegin:!0,contains:[t.inherit(t.TITLE_MODE,{begin:/\w[\w\d_]*/})],relevance:0},R=["if","then","else","elif","fi","time","for","while","until","in","do","done","case","esac","coproc","function","select"],B=["true","false"],j={match:/(\/[a-z._-]+)+/},P=["break","cd","continue","eval","exec","exit","export","getopts","hash","pwd","readonly","return","shift","test","times","trap","umask","unset"],D=["alias","bind","builtin","caller","command","declare","echo","enable","help","let","local","logout","mapfile","printf","read","readarray","source","sudo","type","typeset","ulimit","unalias"],z=["autoload","bg","bindkey","bye","cap","chdir","clone","comparguments","compcall","compctl","compdescribe","compfiles","compgroups","compquote","comptags","comptry","compvalues","dirs","disable","disown","echotc","echoti","emulate","fc","fg","float","functions","getcap","getln","history","integer","jobs","kill","limit","log","noglob","popd","print","pushd","pushln","rehash","sched","setcap","setopt","stat","suspend","ttyctl","unfunction","unhash","unlimit","unsetopt","vared","wait","whence","where","which","zcompile","zformat","zftp","zle","zmodload","zparseopts","zprof","zpty","zregexparse","zsocket","zstyle","ztcp"],Y=["chcon","chgrp","chown","chmod","cp","dd","df","dir","dircolors","ln","ls","mkdir","mkfifo","mknod","mktemp","mv","realpath","rm","rmdir","shred","sync","touch","truncate","vdir","b2sum","base32","base64","cat","cksum","comm","csplit","cut","expand","fmt","fold","head","join","md5sum","nl","numfmt","od","paste","ptx","pr","sha1sum","sha224sum","sha256sum","sha384sum","sha512sum","shuf","sort","split","sum","tac","tail","tr","tsort","unexpand","uniq","wc","arch","basename","chroot","date","dirname","du","echo","env","expr","factor","groups","hostid","id","link","logname","nice","nohup","nproc","pathchk","pinky","printenv","printf","pwd","readlink","runcon","seq","sleep","stat","stdbuf","stty","tee","test","timeout","tty","uname","unlink","uptime","users","who","whoami","yes"];return{name:"Bash",aliases:["sh","zsh"],keywords:{$pattern:/\b[a-z][a-z0-9._-]+\b/,keyword:R,literal:B,built_in:[...P,...D,"set","shopt",...z,...Y]},contains:[y,t.SHEBANG(),E,g,a,r,j,d,o,p,u,n]}}function pr(t){const e=["bool","byte","char","decimal","delegate","double","dynamic","enum","float","int","long","nint","nuint","object","sbyte","short","string","ulong","uint","ushort"],n=["public","private","protected","static","internal","protected","abstract","async","extern","override","unsafe","virtual","new","sealed","partial"],i=["default","false","null","true"],s=["abstract","as","base","break","case","catch","class","const","continue","do","else","event","explicit","extern","finally","fixed","for","foreach","goto","if","implicit","in","interface","internal","is","lock","namespace","new","operator","out","override","params","private","protected","public","readonly","record","ref","return","scoped","sealed","sizeof","stackalloc","static","struct","switch","this","throw","try","typeof","unchecked","unsafe","using","virtual","void","volatile","while"],a=["add","alias","and","ascending","args","async","await","by","descending","dynamic","equals","file","from","get","global","group","init","into","join","let","nameof","not","notnull","on","or","orderby","partial","record","remove","required","scoped","select","set","unmanaged","value|0","var","when","where","with","yield"],r={keyword:s.concat(a),built_in:e,literal:i},d=t.inherit(t.TITLE_MODE,{begin:"[a-zA-Z](\\.?\\w)*"}),o={className:"number",variants:[{begin:"\\b(0b[01']+)"},{begin:"(-?)\\b([\\d']+(\\.[\\d']*)?|\\.[\\d']+)(u|U|l|L|ul|UL|f|F|b|B)"},{begin:"(-?)(\\b0[xX][a-fA-F0-9']+|(\\b[\\d']+(\\.[\\d']*)?|\\.[\\d']+)([eE][-+]?[\\d']+)?)"}],relevance:0},p={className:"string",begin:/"""("*)(?!")(.|\n)*?"""\1/,relevance:1},u={className:"string",begin:'@"',end:'"',contains:[{begin:'""'}]},g=t.inherit(u,{illegal:/\n/}),m={className:"subst",begin:/\{/,end:/\}/,keywords:r},y=t.inherit(m,{illegal:/\n/}),E={className:"string",begin:/\$"/,end:'"',illegal:/\n/,contains:[{begin:/\{\{/},{begin:/\}\}/},t.BACKSLASH_ESCAPE,y]},R={className:"string",begin:/\$@"/,end:'"',contains:[{begin:/\{\{/},{begin:/\}\}/},{begin:'""'},m]},B=t.inherit(R,{illegal:/\n/,contains:[{begin:/\{\{/},{begin:/\}\}/},{begin:'""'},y]});m.contains=[R,E,u,t.APOS_STRING_MODE,t.QUOTE_STRING_MODE,o,t.C_BLOCK_COMMENT_MODE],y.contains=[B,E,g,t.APOS_STRING_MODE,t.QUOTE_STRING_MODE,o,t.inherit(t.C_BLOCK_COMMENT_MODE,{illegal:/\n/})];const j={variants:[p,R,E,u,t.APOS_STRING_MODE,t.QUOTE_STRING_MODE]},P={begin:"<",end:">",contains:[{beginKeywords:"in out"},d]},D=t.IDENT_RE+"(<"+t.IDENT_RE+"(\\s*,\\s*"+t.IDENT_RE+")*>)?(\\[\\])?",z={begin:"@"+t.IDENT_RE,relevance:0};return{name:"C#",aliases:["cs","c#"],keywords:r,illegal:/::/,contains:[t.COMMENT("///","$",{returnBegin:!0,contains:[{className:"doctag",variants:[{begin:"///",relevance:0},{begin:"<!--|-->"},{begin:"</?",end:">"}]}]}),t.C_LINE_COMMENT_MODE,t.C_BLOCK_COMMENT_MODE,{className:"meta",begin:"#",end:"$",keywords:{keyword:"if else elif endif define undef warning error line region endregion pragma checksum"}},j,o,{beginKeywords:"class interface",relevance:0,end:/[{;=]/,illegal:/[^\s:,]/,contains:[{beginKeywords:"where class"},d,P,t.C_LINE_COMMENT_MODE,t.C_BLOCK_COMMENT_MODE]},{beginKeywords:"namespace",relevance:0,end:/[{;=]/,illegal:/[^\s:]/,contains:[d,t.C_LINE_COMMENT_MODE,t.C_BLOCK_COMMENT_MODE]},{beginKeywords:"record",relevance:0,end:/[{;=]/,illegal:/[^\s:]/,contains:[d,P,t.C_LINE_COMMENT_MODE,t.C_BLOCK_COMMENT_MODE]},{className:"meta",begin:"^\\s*\\[(?=[\\w])",excludeBegin:!0,end:"\\]",excludeEnd:!0,contains:[{className:"string",begin:/"/,end:/"/}]},{beginKeywords:"new return throw await else",relevance:0},{className:"function",begin:"("+D+"\\s+)+"+t.IDENT_RE+"\\s*(<[^=]+>\\s*)?\\(",returnBegin:!0,end:/\s*[{;=]/,excludeEnd:!0,keywords:r,contains:[{beginKeywords:n.join(" "),relevance:0},{begin:t.IDENT_RE+"\\s*(<[^=]+>\\s*)?\\(",returnBegin:!0,contains:[t.TITLE_MODE,P],relevance:0},{match:/\(\)/},{className:"params",begin:/\(/,end:/\)/,excludeBegin:!0,excludeEnd:!0,keywords:r,relevance:0,contains:[j,o,t.C_BLOCK_COMMENT_MODE]},t.C_LINE_COMMENT_MODE,t.C_BLOCK_COMMENT_MODE]},z]}}function Vo(t){const e=t.regex;return{name:"Diff",aliases:["patch"],contains:[{className:"meta",relevance:10,match:e.either(/^@@ +-\d+,\d+ +\+\d+,\d+ +@@/,/^\*\*\* +\d+,\d+ +\*\*\*\*$/,/^--- +\d+,\d+ +----$/)},{className:"comment",variants:[{begin:e.either(/Index: /,/^index/,/={3,}/,/^-{3}/,/^\*{3} /,/^\+{3}/,/^diff --git/),end:/$/},{match:/^\*{15}$/}]},{className:"addition",begin:/^\+/,end:/$/},{className:"deletion",begin:/^-/,end:/$/},{className:"addition",begin:/^!/,end:/$/}]}}const Ci="[A-Za-z$_][0-9A-Za-z$_]*",Xo=["as","in","of","if","for","while","finally","var","new","function","do","return","void","else","break","catch","instanceof","with","throw","case","default","try","switch","continue","typeof","delete","let","yield","const","class","debugger","async","await","static","import","from","export","extends","using"],Yo=["true","false","null","undefined","NaN","Infinity"],ur=["Object","Function","Boolean","Symbol","Math","Date","Number","BigInt","String","RegExp","Array","Float32Array","Float64Array","Int8Array","Uint8Array","Uint8ClampedArray","Int16Array","Int32Array","Uint16Array","Uint32Array","BigInt64Array","BigUint64Array","Set","Map","WeakSet","WeakMap","ArrayBuffer","SharedArrayBuffer","Atomics","DataView","JSON","Promise","Generator","GeneratorFunction","AsyncFunction","Reflect","Proxy","Intl","WebAssembly"],hr=["Error","EvalError","InternalError","RangeError","ReferenceError","SyntaxError","TypeError","URIError"],gr=["setInterval","setTimeout","clearInterval","clearTimeout","require","exports","eval","isFinite","isNaN","parseFloat","parseInt","decodeURI","decodeURIComponent","encodeURI","encodeURIComponent","escape","unescape"],Qo=["arguments","this","super","console","window","document","localStorage","sessionStorage","module","global"],Jo=[].concat(gr,ur,hr);function fr(t){const e=t.regex,n=(H,{after:he})=>{const be="</"+H[0].slice(1);return H.input.indexOf(be,he)!==-1},i=Ci,s={begin:"<>",end:"</>"},a=/<[A-Za-z0-9\\._:-]+\s*\/>/,r={begin:/<[A-Za-z0-9\\._:-]+/,end:/\/[A-Za-z0-9\\._:-]+>|\/>/,isTrulyOpeningTag:(H,he)=>{const be=H[0].length+H.index,q=H.input[be];if(q==="<"||q===","){he.ignoreMatch();return}q===">"&&(n(H,{after:be})||he.ignoreMatch());let Ne;const J=H.input.substring(be);if(Ne=J.match(/^\s*=/)){he.ignoreMatch();return}if((Ne=J.match(/^\s+extends\s+/))&&Ne.index===0){he.ignoreMatch();return}}},d={$pattern:Ci,keyword:Xo,literal:Yo,built_in:Jo,"variable.language":Qo},o="[0-9](_?[0-9])*",p=`\\.(${o})`,u="0|[1-9](_?[0-9])*|0[0-7]*[89][0-9]*",g={className:"number",variants:[{begin:`(\\b(${u})((${p})|\\.)?|(${p}))[eE][+-]?(${o})\\b`},{begin:`\\b(${u})\\b((${p})\\b|\\.)?|(${p})\\b`},{begin:"\\b(0|[1-9](_?[0-9])*)n\\b"},{begin:"\\b0[xX][0-9a-fA-F](_?[0-9a-fA-F])*n?\\b"},{begin:"\\b0[bB][0-1](_?[0-1])*n?\\b"},{begin:"\\b0[oO][0-7](_?[0-7])*n?\\b"},{begin:"\\b0[0-7]+n?\\b"}],relevance:0},m={className:"subst",begin:"\\$\\{",end:"\\}",keywords:d,contains:[]},y={begin:".?html`",end:"",starts:{end:"`",returnEnd:!1,contains:[t.BACKSLASH_ESCAPE,m],subLanguage:"xml"}},E={begin:".?css`",end:"",starts:{end:"`",returnEnd:!1,contains:[t.BACKSLASH_ESCAPE,m],subLanguage:"css"}},R={begin:".?gql`",end:"",starts:{end:"`",returnEnd:!1,contains:[t.BACKSLASH_ESCAPE,m],subLanguage:"graphql"}},B={className:"string",begin:"`",end:"`",contains:[t.BACKSLASH_ESCAPE,m]},P={className:"comment",variants:[t.COMMENT(/\/\*\*(?!\/)/,"\\*/",{relevance:0,contains:[{begin:"(?=@[A-Za-z]+)",relevance:0,contains:[{className:"doctag",begin:"@[A-Za-z]+"},{className:"type",begin:"\\{",end:"\\}",excludeEnd:!0,excludeBegin:!0,relevance:0},{className:"variable",begin:i+"(?=\\s*(-)|$)",endsParent:!0,relevance:0},{begin:/(?=[^\n])\s/,relevance:0}]}]}),t.C_BLOCK_COMMENT_MODE,t.C_LINE_COMMENT_MODE]},D=[t.APOS_STRING_MODE,t.QUOTE_STRING_MODE,y,E,R,B,{match:/\$\d+/},g];m.contains=D.concat({begin:/\{/,end:/\}/,keywords:d,contains:["self"].concat(D)});const z=[].concat(P,m.contains),Y=z.concat([{begin:/(\s*)\(/,end:/\)/,keywords:d,contains:["self"].concat(z)}]),te={className:"params",begin:/(\s*)\(/,end:/\)/,excludeBegin:!0,excludeEnd:!0,keywords:d,contains:Y},tt={variants:[{match:[/class/,/\s+/,i,/\s+/,/extends/,/\s+/,e.concat(i,"(",e.concat(/\./,i),")*")],scope:{1:"keyword",3:"title.class",5:"keyword",7:"title.class.inherited"}},{match:[/class/,/\s+/,i],scope:{1:"keyword",3:"title.class"}}]},Le={relevance:0,match:e.either(/\bJSON/,/\b[A-Z][a-z]+([A-Z][a-z]*|\d)*/,/\b[A-Z]{2,}([A-Z][a-z]+|\d)+([A-Z][a-z]*)*/,/\b[A-Z]{2,}[a-z]+([A-Z][a-z]+|\d)*([A-Z][a-z]*)*/),className:"title.class",keywords:{_:[...ur,...hr]}},nt={label:"use_strict",className:"meta",relevance:10,begin:/^\s*['"]use (strict|asm)['"]/},st={variants:[{match:[/function/,/\s+/,i,/(?=\s*\()/]},{match:[/function/,/\s*(?=\()/]}],className:{1:"keyword",3:"title.function"},label:"func.def",contains:[te],illegal:/%/},ie={relevance:0,match:/\b[A-Z][A-Z_0-9]+\b/,className:"variable.constant"};function Ve(H){return e.concat("(?!",H.join("|"),")")}const Xe={match:e.concat(/\b/,Ve([...gr,"super","import"].map(H=>`${H}\\s*\\(`)),i,e.lookahead(/\s*\(/)),className:"title.function",relevance:0},Ae={begin:e.concat(/\./,e.lookahead(e.concat(i,/(?![0-9A-Za-z$_(])/))),end:i,excludeBegin:!0,keywords:"prototype",className:"property",relevance:0},bt={match:[/get|set/,/\s+/,i,/(?=\()/],className:{1:"keyword",3:"title.function"},contains:[{begin:/\(\)/},te]},Ye="(\\([^()]*(\\([^()]*(\\([^()]*\\)[^()]*)*\\)[^()]*)*\\)|"+t.UNDERSCORE_IDENT_RE+")\\s*=>",mt={match:[/const|var|let/,/\s+/,i,/\s*/,/=\s*/,/(async\s*)?/,e.lookahead(Ye)],keywords:"async",className:{1:"keyword",3:"title.function"},contains:[te]};return{name:"JavaScript",aliases:["js","jsx","mjs","cjs"],keywords:d,exports:{PARAMS_CONTAINS:Y,CLASS_REFERENCE:Le},illegal:/#(?![$_A-z])/,contains:[t.SHEBANG({label:"shebang",binary:"node",relevance:5}),nt,t.APOS_STRING_MODE,t.QUOTE_STRING_MODE,y,E,R,B,P,{match:/\$\d+/},g,Le,{scope:"attr",match:i+e.lookahead(":"),relevance:0},mt,{begin:"("+t.RE_STARTERS_RE+"|\\b(case|return|throw)\\b)\\s*",keywords:"return throw case",relevance:0,contains:[P,t.REGEXP_MODE,{className:"function",begin:Ye,returnBegin:!0,end:"\\s*=>",contains:[{className:"params",variants:[{begin:t.UNDERSCORE_IDENT_RE,relevance:0},{className:null,begin:/\(\s*\)/,skip:!0},{begin:/(\s*)\(/,end:/\)/,excludeBegin:!0,excludeEnd:!0,keywords:d,contains:Y}]}]},{begin:/,/,relevance:0},{match:/\s+/,relevance:0},{variants:[{begin:s.begin,end:s.end},{match:a},{begin:r.begin,"on:begin":r.isTrulyOpeningTag,end:r.end}],subLanguage:"xml",contains:[{begin:r.begin,end:r.end,skip:!0,contains:["self"]}]}]},st,{beginKeywords:"while if switch catch for"},{begin:"\\b(?!function)"+t.UNDERSCORE_IDENT_RE+"\\([^()]*(\\([^()]*(\\([^()]*\\)[^()]*)*\\)[^()]*)*\\)\\s*\\{",returnBegin:!0,label:"func.def",contains:[te,t.inherit(t.TITLE_MODE,{begin:i,className:"title.function"})]},{match:/\.\.\./,relevance:0},Ae,{match:"\\$"+i,relevance:0},{match:[/\bconstructor(?=\s*\()/],className:{1:"title.function"},contains:[te]},Xe,ie,tt,bt,{match:/\$[(.]/}]}}function el(t){const e={className:"attr",begin:/"(\\.|[^\\"\r\n])*"(?=\s*:)/,relevance:1.01},n={match:/[{}[\],:]/,className:"punctuation",relevance:0},i=["true","false","null"],s={scope:"literal",beginKeywords:i.join(" ")};return{name:"JSON",aliases:["jsonc"],keywords:{literal:i},contains:[e,n,t.QUOTE_STRING_MODE,s,t.C_NUMBER_MODE,t.C_LINE_COMMENT_MODE,t.C_BLOCK_COMMENT_MODE],illegal:"\\S"}}function br(t){return{name:"Plain text",aliases:["text","txt"],disableAutodetect:!0}}function mr(t){const e=["string","char","byte","int","long","bool","decimal","single","double","DateTime","xml","array","hashtable","void"],n="Add|Clear|Close|Copy|Enter|Exit|Find|Format|Get|Hide|Join|Lock|Move|New|Open|Optimize|Pop|Push|Redo|Remove|Rename|Reset|Resize|Search|Select|Set|Show|Skip|Split|Step|Switch|Undo|Unlock|Watch|Backup|Checkpoint|Compare|Compress|Convert|ConvertFrom|ConvertTo|Dismount|Edit|Expand|Export|Group|Import|Initialize|Limit|Merge|Mount|Out|Publish|Restore|Save|Sync|Unpublish|Update|Approve|Assert|Build|Complete|Confirm|Deny|Deploy|Disable|Enable|Install|Invoke|Register|Request|Restart|Resume|Start|Stop|Submit|Suspend|Uninstall|Unregister|Wait|Debug|Measure|Ping|Repair|Resolve|Test|Trace|Connect|Disconnect|Read|Receive|Send|Write|Block|Grant|Protect|Revoke|Unblock|Unprotect|Use|ForEach|Sort|Tee|Where",i="-and|-as|-band|-bnot|-bor|-bxor|-casesensitive|-ccontains|-ceq|-cge|-cgt|-cle|-clike|-clt|-cmatch|-cne|-cnotcontains|-cnotlike|-cnotmatch|-contains|-creplace|-csplit|-eq|-exact|-f|-file|-ge|-gt|-icontains|-ieq|-ige|-igt|-ile|-ilike|-ilt|-imatch|-in|-ine|-inotcontains|-inotlike|-inotmatch|-ireplace|-is|-isnot|-isplit|-join|-le|-like|-lt|-match|-ne|-not|-notcontains|-notin|-notlike|-notmatch|-or|-regex|-replace|-shl|-shr|-split|-wildcard|-xor",s={$pattern:/-?[A-z\.\-]+\b/,keyword:"if else foreach return do while until elseif begin for trap data dynamicparam end break throw param continue finally in switch exit filter try process catch hidden static parameter",built_in:"ac asnp cat cd CFS chdir clc clear clhy cli clp cls clv cnsn compare copy cp cpi cpp curl cvpa dbp del diff dir dnsn ebp echo|0 epal epcsv epsn erase etsn exsn fc fhx fl ft fw gal gbp gc gcb gci gcm gcs gdr gerr ghy gi gin gjb gl gm gmo gp gps gpv group gsn gsnp gsv gtz gu gv gwmi h history icm iex ihy ii ipal ipcsv ipmo ipsn irm ise iwmi iwr kill lp ls man md measure mi mount move mp mv nal ndr ni nmo npssc nsn nv ogv oh popd ps pushd pwd r rbp rcjb rcsn rd rdr ren ri rjb rm rmdir rmo rni rnp rp rsn rsnp rujb rv rvpa rwmi sajb sal saps sasv sbp sc scb select set shcm si sl sleep sls sort sp spjb spps spsv start stz sujb sv swmi tee trcm type wget where wjb write"},a=/\w[\w\d]*((-)[\w\d]+)*/,r={begin:"`[\\s\\S]",relevance:0},d={className:"variable",variants:[{begin:/\$\B/},{className:"keyword",begin:/\$this/},{begin:/\$[\w\d][\w\d_:]*/}]},o={className:"literal",begin:/\$(null|true|false)\b/},p={className:"string",variants:[{begin:/"/,end:/"/},{begin:/@"/,end:/^"@/}],contains:[r,d,{className:"variable",begin:/\$[A-z]/,end:/[^A-z]/}]},u={className:"string",variants:[{begin:/'/,end:/'/},{begin:/@'/,end:/^'@/}]},g={className:"doctag",variants:[{begin:/\.(synopsis|description|example|inputs|outputs|notes|link|component|role|functionality)/},{begin:/\.(parameter|forwardhelptargetname|forwardhelpcategory|remotehelprunspace|externalhelp)\s+\S+/}]},m=t.inherit(t.COMMENT(null,null),{variants:[{begin:/#/,end:/$/},{begin:/<#/,end:/#>/}],contains:[g]}),y={className:"built_in",variants:[{begin:"(".concat(n,")+(-)[\\w\\d]+")}]},E={className:"class",beginKeywords:"class enum",end:/\s*[{]/,excludeEnd:!0,relevance:0,contains:[t.TITLE_MODE]},R={className:"function",begin:/function\s+/,end:/\s*\{|$/,excludeEnd:!0,returnBegin:!0,relevance:0,contains:[{begin:"function",relevance:0,className:"keyword"},{className:"title",begin:a,relevance:0},{begin:/\(/,end:/\)/,className:"params",relevance:0,contains:[d]}]},B={begin:/using\s/,end:/$/,returnBegin:!0,contains:[p,u,{className:"keyword",begin:/(using|assembly|command|module|namespace|type)/}]},j={variants:[{className:"operator",begin:"(".concat(i,")\\b")},{className:"literal",begin:/(-){1,2}[\w\d-]+/,relevance:0}]},P={className:"selector-tag",begin:/@\B/,relevance:0},D={className:"function",begin:/\[.*\]\s*[\w]+[ ]??\(/,end:/$/,returnBegin:!0,relevance:0,contains:[{className:"keyword",begin:"(".concat(s.keyword.toString().replace(/\s/g,"|"),")\\b"),endsParent:!0,relevance:0},t.inherit(t.TITLE_MODE,{endsParent:!0})]},z=[D,m,r,t.NUMBER_MODE,p,u,y,d,o,P],Y={begin:/\[/,end:/\]/,excludeBegin:!0,excludeEnd:!0,relevance:0,contains:[].concat("self",z,{begin:"("+e.join("|")+")",className:"built_in",relevance:0},{className:"type",begin:/[\.\w\d]+/,relevance:0})};return D.contains.unshift(Y),{name:"PowerShell",aliases:["pwsh","ps","ps1"],case_insensitive:!0,keywords:s,contains:z.concat(E,R,B,j,Y)}}const es="[A-Za-z$_][0-9A-Za-z$_]*",_r=["as","in","of","if","for","while","finally","var","new","function","do","return","void","else","break","catch","instanceof","with","throw","case","default","try","switch","continue","typeof","delete","let","yield","const","class","debugger","async","await","static","import","from","export","extends","using"],xr=["true","false","null","undefined","NaN","Infinity"],yr=["Object","Function","Boolean","Symbol","Math","Date","Number","BigInt","String","RegExp","Array","Float32Array","Float64Array","Int8Array","Uint8Array","Uint8ClampedArray","Int16Array","Int32Array","Uint16Array","Uint32Array","BigInt64Array","BigUint64Array","Set","Map","WeakSet","WeakMap","ArrayBuffer","SharedArrayBuffer","Atomics","DataView","JSON","Promise","Generator","GeneratorFunction","AsyncFunction","Reflect","Proxy","Intl","WebAssembly"],wr=["Error","EvalError","InternalError","RangeError","ReferenceError","SyntaxError","TypeError","URIError"],Er=["setInterval","setTimeout","clearInterval","clearTimeout","require","exports","eval","isFinite","isNaN","parseFloat","parseInt","decodeURI","decodeURIComponent","encodeURI","encodeURIComponent","escape","unescape"],vr=["arguments","this","super","console","window","document","localStorage","sessionStorage","module","global"],kr=[].concat(Er,yr,wr);function tl(t){const e=t.regex,n=(H,{after:he})=>{const be="</"+H[0].slice(1);return H.input.indexOf(be,he)!==-1},i=es,s={begin:"<>",end:"</>"},a=/<[A-Za-z0-9\\._:-]+\s*\/>/,r={begin:/<[A-Za-z0-9\\._:-]+/,end:/\/[A-Za-z0-9\\._:-]+>|\/>/,isTrulyOpeningTag:(H,he)=>{const be=H[0].length+H.index,q=H.input[be];if(q==="<"||q===","){he.ignoreMatch();return}q===">"&&(n(H,{after:be})||he.ignoreMatch());let Ne;const J=H.input.substring(be);if(Ne=J.match(/^\s*=/)){he.ignoreMatch();return}if((Ne=J.match(/^\s+extends\s+/))&&Ne.index===0){he.ignoreMatch();return}}},d={$pattern:es,keyword:_r,literal:xr,built_in:kr,"variable.language":vr},o="[0-9](_?[0-9])*",p=`\\.(${o})`,u="0|[1-9](_?[0-9])*|0[0-7]*[89][0-9]*",g={className:"number",variants:[{begin:`(\\b(${u})((${p})|\\.)?|(${p}))[eE][+-]?(${o})\\b`},{begin:`\\b(${u})\\b((${p})\\b|\\.)?|(${p})\\b`},{begin:"\\b(0|[1-9](_?[0-9])*)n\\b"},{begin:"\\b0[xX][0-9a-fA-F](_?[0-9a-fA-F])*n?\\b"},{begin:"\\b0[bB][0-1](_?[0-1])*n?\\b"},{begin:"\\b0[oO][0-7](_?[0-7])*n?\\b"},{begin:"\\b0[0-7]+n?\\b"}],relevance:0},m={className:"subst",begin:"\\$\\{",end:"\\}",keywords:d,contains:[]},y={begin:".?html`",end:"",starts:{end:"`",returnEnd:!1,contains:[t.BACKSLASH_ESCAPE,m],subLanguage:"xml"}},E={begin:".?css`",end:"",starts:{end:"`",returnEnd:!1,contains:[t.BACKSLASH_ESCAPE,m],subLanguage:"css"}},R={begin:".?gql`",end:"",starts:{end:"`",returnEnd:!1,contains:[t.BACKSLASH_ESCAPE,m],subLanguage:"graphql"}},B={className:"string",begin:"`",end:"`",contains:[t.BACKSLASH_ESCAPE,m]},P={className:"comment",variants:[t.COMMENT(/\/\*\*(?!\/)/,"\\*/",{relevance:0,contains:[{begin:"(?=@[A-Za-z]+)",relevance:0,contains:[{className:"doctag",begin:"@[A-Za-z]+"},{className:"type",begin:"\\{",end:"\\}",excludeEnd:!0,excludeBegin:!0,relevance:0},{className:"variable",begin:i+"(?=\\s*(-)|$)",endsParent:!0,relevance:0},{begin:/(?=[^\n])\s/,relevance:0}]}]}),t.C_BLOCK_COMMENT_MODE,t.C_LINE_COMMENT_MODE]},D=[t.APOS_STRING_MODE,t.QUOTE_STRING_MODE,y,E,R,B,{match:/\$\d+/},g];m.contains=D.concat({begin:/\{/,end:/\}/,keywords:d,contains:["self"].concat(D)});const z=[].concat(P,m.contains),Y=z.concat([{begin:/(\s*)\(/,end:/\)/,keywords:d,contains:["self"].concat(z)}]),te={className:"params",begin:/(\s*)\(/,end:/\)/,excludeBegin:!0,excludeEnd:!0,keywords:d,contains:Y},tt={variants:[{match:[/class/,/\s+/,i,/\s+/,/extends/,/\s+/,e.concat(i,"(",e.concat(/\./,i),")*")],scope:{1:"keyword",3:"title.class",5:"keyword",7:"title.class.inherited"}},{match:[/class/,/\s+/,i],scope:{1:"keyword",3:"title.class"}}]},Le={relevance:0,match:e.either(/\bJSON/,/\b[A-Z][a-z]+([A-Z][a-z]*|\d)*/,/\b[A-Z]{2,}([A-Z][a-z]+|\d)+([A-Z][a-z]*)*/,/\b[A-Z]{2,}[a-z]+([A-Z][a-z]+|\d)*([A-Z][a-z]*)*/),className:"title.class",keywords:{_:[...yr,...wr]}},nt={label:"use_strict",className:"meta",relevance:10,begin:/^\s*['"]use (strict|asm)['"]/},st={variants:[{match:[/function/,/\s+/,i,/(?=\s*\()/]},{match:[/function/,/\s*(?=\()/]}],className:{1:"keyword",3:"title.function"},label:"func.def",contains:[te],illegal:/%/},ie={relevance:0,match:/\b[A-Z][A-Z_0-9]+\b/,className:"variable.constant"};function Ve(H){return e.concat("(?!",H.join("|"),")")}const Xe={match:e.concat(/\b/,Ve([...Er,"super","import"].map(H=>`${H}\\s*\\(`)),i,e.lookahead(/\s*\(/)),className:"title.function",relevance:0},Ae={begin:e.concat(/\./,e.lookahead(e.concat(i,/(?![0-9A-Za-z$_(])/))),end:i,excludeBegin:!0,keywords:"prototype",className:"property",relevance:0},bt={match:[/get|set/,/\s+/,i,/(?=\()/],className:{1:"keyword",3:"title.function"},contains:[{begin:/\(\)/},te]},Ye="(\\([^()]*(\\([^()]*(\\([^()]*\\)[^()]*)*\\)[^()]*)*\\)|"+t.UNDERSCORE_IDENT_RE+")\\s*=>",mt={match:[/const|var|let/,/\s+/,i,/\s*/,/=\s*/,/(async\s*)?/,e.lookahead(Ye)],keywords:"async",className:{1:"keyword",3:"title.function"},contains:[te]};return{name:"JavaScript",aliases:["js","jsx","mjs","cjs"],keywords:d,exports:{PARAMS_CONTAINS:Y,CLASS_REFERENCE:Le},illegal:/#(?![$_A-z])/,contains:[t.SHEBANG({label:"shebang",binary:"node",relevance:5}),nt,t.APOS_STRING_MODE,t.QUOTE_STRING_MODE,y,E,R,B,P,{match:/\$\d+/},g,Le,{scope:"attr",match:i+e.lookahead(":"),relevance:0},mt,{begin:"("+t.RE_STARTERS_RE+"|\\b(case|return|throw)\\b)\\s*",keywords:"return throw case",relevance:0,contains:[P,t.REGEXP_MODE,{className:"function",begin:Ye,returnBegin:!0,end:"\\s*=>",contains:[{className:"params",variants:[{begin:t.UNDERSCORE_IDENT_RE,relevance:0},{className:null,begin:/\(\s*\)/,skip:!0},{begin:/(\s*)\(/,end:/\)/,excludeBegin:!0,excludeEnd:!0,keywords:d,contains:Y}]}]},{begin:/,/,relevance:0},{match:/\s+/,relevance:0},{variants:[{begin:s.begin,end:s.end},{match:a},{begin:r.begin,"on:begin":r.isTrulyOpeningTag,end:r.end}],subLanguage:"xml",contains:[{begin:r.begin,end:r.end,skip:!0,contains:["self"]}]}]},st,{beginKeywords:"while if switch catch for"},{begin:"\\b(?!function)"+t.UNDERSCORE_IDENT_RE+"\\([^()]*(\\([^()]*(\\([^()]*\\)[^()]*)*\\)[^()]*)*\\)\\s*\\{",returnBegin:!0,label:"func.def",contains:[te,t.inherit(t.TITLE_MODE,{begin:i,className:"title.function"})]},{match:/\.\.\./,relevance:0},Ae,{match:"\\$"+i,relevance:0},{match:[/\bconstructor(?=\s*\()/],className:{1:"title.function"},contains:[te]},Xe,ie,tt,bt,{match:/\$[(.]/}]}}function Sr(t){const e=t.regex,n=tl(t),i=es,s=["any","void","number","boolean","string","object","never","symbol","bigint","unknown"],a={begin:[/namespace/,/\s+/,t.IDENT_RE],beginScope:{1:"keyword",3:"title.class"}},r={beginKeywords:"interface",end:/\{/,excludeEnd:!0,keywords:{keyword:"interface extends",built_in:s},contains:[n.exports.CLASS_REFERENCE]},d={className:"meta",relevance:10,begin:/^\s*['"]use strict['"]/},o=["type","interface","public","private","protected","implements","declare","abstract","readonly","enum","override","satisfies"],p={$pattern:es,keyword:_r.concat(o),literal:xr,built_in:kr.concat(s),"variable.language":vr},u={className:"meta",begin:"@"+i},g=(R,B,j)=>{const P=R.contains.findIndex(D=>D.label===B);if(P===-1)throw new Error("can not find mode to replace");R.contains.splice(P,1,j)};Object.assign(n.keywords,p),n.exports.PARAMS_CONTAINS.push(u);const m=n.contains.find(R=>R.scope==="attr"),y=Object.assign({},m,{match:e.concat(i,e.lookahead(/\s*\?:/))});n.exports.PARAMS_CONTAINS.push([n.exports.CLASS_REFERENCE,m,y]),n.contains=n.contains.concat([u,a,r,y]),g(n,"shebang",t.SHEBANG()),g(n,"use_strict",d);const E=n.contains.find(R=>R.label==="func.def");return E.relevance=0,Object.assign(n,{name:"TypeScript",aliases:["ts","tsx","mts","cts"]}),n}function Tr(t){const e=t.regex,n=e.concat(/[\p{L}_]/u,e.optional(/[\p{L}0-9_.-]*:/u),/[\p{L}0-9_.-]*/u),i=/[\p{L}0-9._:-]+/u,s={className:"symbol",begin:/&[a-z]+;|&#[0-9]+;|&#x[a-f0-9]+;/},a={begin:/\s/,contains:[{className:"keyword",begin:/#?[a-z_][a-z1-9_-]+/,illegal:/\n/}]},r=t.inherit(a,{begin:/\(/,end:/\)/}),d=t.inherit(t.APOS_STRING_MODE,{className:"string"}),o=t.inherit(t.QUOTE_STRING_MODE,{className:"string"}),p={endsWithParent:!0,illegal:/</,relevance:0,contains:[{className:"attr",begin:i,relevance:0},{begin:/=\s*/,relevance:0,contains:[{className:"string",endsParent:!0,variants:[{begin:/"/,end:/"/,contains:[s]},{begin:/'/,end:/'/,contains:[s]},{begin:/[^\s"'=<>`]+/}]}]}]};return{name:"HTML, XML",aliases:["html","xhtml","rss","atom","xjb","xsd","xsl","plist","wsf","svg"],case_insensitive:!0,unicodeRegex:!0,contains:[{className:"meta",begin:/<![a-z]/,end:/>/,relevance:10,contains:[a,o,d,r,{begin:/\[/,end:/\]/,contains:[{className:"meta",begin:/<![a-z]/,end:/>/,contains:[a,r,o,d]}]}]},t.COMMENT(/<!--/,/-->/,{relevance:10}),{begin:/<!\[CDATA\[/,end:/\]\]>/,relevance:10},s,{className:"meta",end:/\?>/,variants:[{begin:/<\?xml/,relevance:10,contains:[o]},{begin:/<\?[a-z][a-z0-9]+/}]},{className:"tag",begin:/<style(?=\s|>)/,end:/>/,keywords:{name:"style"},contains:[p],starts:{end:/<\/style>/,returnEnd:!0,subLanguage:["css","xml"]}},{className:"tag",begin:/<script(?=\s|>)/,end:/>/,keywords:{name:"script"},contains:[p],starts:{end:/<\/script>/,returnEnd:!0,subLanguage:["javascript","handlebars","xml"]}},{className:"tag",begin:/<>|<\/>/},{className:"tag",begin:e.concat(/</,e.lookahead(e.concat(n,e.either(/\/>/,/>/,/\s/)))),end:/\/?>/,contains:[{className:"name",begin:n,relevance:0,starts:p}]},{className:"tag",begin:e.concat(/<\//,e.lookahead(e.concat(n,/>/))),contains:[{className:"name",begin:n,relevance:0},{begin:/>/,relevance:0,endsParent:!0}]}]}}const nl="pre code.hljs{display:block;overflow-x:auto;padding:1em}code.hljs{padding:3px 5px}.hljs{color:#c9d1d9;background:#0d1117}.hljs-doctag,.hljs-keyword,.hljs-meta .hljs-keyword,.hljs-template-tag,.hljs-template-variable,.hljs-type,.hljs-variable.language_{color:#ff7b72}.hljs-title,.hljs-title.class_,.hljs-title.class_.inherited__,.hljs-title.function_{color:#d2a8ff}.hljs-attr,.hljs-attribute,.hljs-literal,.hljs-meta,.hljs-number,.hljs-operator,.hljs-variable,.hljs-selector-attr,.hljs-selector-class,.hljs-selector-id{color:#79c0ff}.hljs-regexp,.hljs-string,.hljs-meta .hljs-string{color:#a5d6ff}.hljs-built_in,.hljs-symbol{color:#ffa657}.hljs-comment,.hljs-code,.hljs-formula{color:#8b949e}.hljs-name,.hljs-quote,.hljs-selector-tag,.hljs-selector-pseudo{color:#7ee787}.hljs-subst{color:#c9d1d9}.hljs-section{color:#1f6feb;font-weight:700}.hljs-bullet{color:#f2cc60}.hljs-emphasis{color:#c9d1d9;font-style:italic}.hljs-strong{color:#c9d1d9;font-weight:700}.hljs-addition{color:#aff5b4;background-color:#033a16}.hljs-deletion{color:#ffdcd7;background-color:#67060c}";var sl=Object.defineProperty,il=Object.getOwnPropertyDescriptor,cs=(t,e,n,i)=>{for(var s=i>1?void 0:i?il(e,n):e,a=t.length-1,r;a>=0;a--)(r=t[a])&&(s=(i?r(e,n,s):r(s))||s);return i&&s&&sl(e,n,s),s};de.registerLanguage("bash",qs);de.registerLanguage("sh",qs);de.registerLanguage("shell",qs);de.registerLanguage("powershell",mr);de.registerLanguage("ps1",mr);de.registerLanguage("json",el);de.registerLanguage("javascript",fr);de.registerLanguage("js",fr);de.registerLanguage("typescript",Sr);de.registerLanguage("ts",Sr);de.registerLanguage("xml",Tr);de.registerLanguage("html",Tr);de.registerLanguage("csharp",pr);de.registerLanguage("cs",pr);de.registerLanguage("diff",Vo);de.registerLanguage("plaintext",br);de.registerLanguage("text",br);F.setOptions({async:!1});const Ar=new F.Renderer;Ar.code=({text:t,lang:e})=>{const n=e&&de.getLanguage(e)?e:"plaintext",i=de.highlight(t,{language:n}).value;return`<pre><code class="hljs language-${n}">${i}</code></pre>`};F.use({renderer:Ar});let Zt=class extends Te{constructor(){super(...arguments),this.role="user",this.content="",this.reasoning=!1}get _html(){if(this.reasoning)return T`<span>${this.content}</span>`;const t=this.content.replace(/<thinking>[\s\S]*?<\/thinking>/gi,"").trim(),e=F.parse(t),n=to.sanitize(e,{FORBID_TAGS:["script","style","iframe","object","embed"]});return Zi(n)}render(){return T`
      <div class="bubble ${this.role}">
        <div class="meta-row">
          <span class="meta">${this.role==="user"?"Du":"bashGPT"}</span>
        </div>
        ${this.reasoning?T`<div class="reasoning-label">Denkt…</div>`:""}
        <div class="content ${this.reasoning?"is-reasoning":""}">${this._html}</div>
      </div>
    `}};Zt.styles=[Li(nl),Ke`
      :host { display: block; margin-bottom: 12px; }

      .bubble {
        border-radius: 12px;
        padding: 12px 16px;
        border: 1px solid var(--color-border, #374151);
        line-height: 1.6;
      }

      .bubble.user {
        background: var(--color-user, #1f2937);
        margin-left: 40px;
      }

      .bubble.assistant {
        background: var(--color-assistant, #0b1220);
        margin-right: 40px;
      }

      .meta {
        font-size: 11px;
        color: var(--color-muted, #6b7280);
        margin-bottom: 6px;
        text-transform: uppercase;
        letter-spacing: 0.05em;
      }

      .content { color: var(--color-text, #e5e7eb); }

      /* Reasoning-Modus: gedämpfte Darstellung */
      .reasoning-label {
        font-size: 10px;
        color: #475569;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        margin-bottom: 4px;
      }
      .content.is-reasoning {
        color: #64748b;
        font-style: italic;
        white-space: pre-wrap;
      }

      /* Markdown-Stile */
      .content pre {
        background: #020617;
        border: 1px solid #1e293b;
        border-radius: 8px;
        padding: 12px;
        overflow-x: auto;
        margin: 10px 0;
      }
      .content code:not(pre code) {
        background: #1e293b;
        border-radius: 4px;
        padding: 2px 5px;
        font-size: 0.88em;
        color: #93c5fd;
      }
      .content p { margin: 6px 0; }
      .content ul, .content ol { padding-left: 20px; margin: 6px 0; }
      .content h1, .content h2, .content h3 {
        margin: 10px 0 4px;
        color: #f1f5f9;
      }
      .content a { color: #38bdf8; }
      .content blockquote {
        border-left: 3px solid #475569;
        margin: 8px 0;
        padding-left: 12px;
        color: #94a3b8;
      }
      .content table { border-collapse: collapse; width: 100%; margin: 8px 0; }
      .content th, .content td {
        border: 1px solid #374151;
        padding: 6px 10px;
        text-align: left;
      }
      .content th { background: #1e293b; }

      /* Exec-mode badge */
      .meta-row {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 6px;
      }
    `];cs([fe()],Zt.prototype,"role",2);cs([fe()],Zt.prototype,"content",2);cs([fe({type:Boolean})],Zt.prototype,"reasoning",2);Zt=cs([Ze("bashgpt-message")],Zt);var rl=Object.defineProperty,al=Object.getOwnPropertyDescriptor,Ks=(t,e,n,i)=>{for(var s=i>1?void 0:i?al(e,n):e,a=t.length-1,r;a>=0;a--)(r=t[a])&&(s=(i?r(e,n,s):r(s))||s);return i&&s&&rl(e,n,s),s};let En=class extends Te{constructor(){super(...arguments),this.entries=[],this.loading=!1}updated(){const t=this.shadowRoot?.querySelector(".entries");t&&(t.scrollTop=t.scrollHeight)}_badgeClass(t){return{running:"badge-running",success:"badge-success",error:"badge-error",skipped:"badge-skipped",timeout:"badge-timeout",user_cancelled:"badge-user-cancelled"}[t]}_badgeLabel(t){return t.status==="running"?"running":t.status==="skipped"?"skipped":t.status==="timeout"?"timeout":t.status==="user_cancelled"?"cancelled":t.status==="success"?"ok":`exit ${t.exitCode}`}_outputClass(t){return t.status==="skipped"?"skipped":t.status==="error"||t.status==="timeout"||t.status==="user_cancelled"?"error":""}render(){const t=this.entries.length>0||this.loading;return T`
      <div class="panel-header">
        <div class="panel-title">Tool Calls</div>
        <div class="panel-count">${this.entries.length}</div>
      </div>

      <div class="entries">
        ${t?"":T`<div class="empty">No tool calls yet.</div>`}

        ${vn(this.entries,(e,n)=>n,e=>T`
            <div class="entry">
              <div class="entry-head">
                <span class="tool-name">${e.toolName||"tool"}</span>
                <span class="status-badge ${this._badgeClass(e.status)}">${this._badgeLabel(e)}</span>
              </div>
              <div class="entry-body">
                <div>
                  <div class="label">Command</div>
                  <pre class="command">${e.command}</pre>
                </div>
                ${e.status==="running"?T``:T`
                    <div>
                      <div class="label">Output</div>
                      <pre class="output ${this._outputClass(e)}">${e.output?.length?e.output:"(no output)"}</pre>
                    </div>
                  `}
              </div>
            </div>
          `)}
      </div>
    `}};En.styles=Ke`
    :host {
      display: flex;
      flex-direction: column;
      background: #020617;
      border-right: 1px solid #1e293b;
      overflow: hidden;
    }

    .panel-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      padding: 10px 12px;
      border-bottom: 1px solid #1e293b;
      background: #0b1120;
      flex-shrink: 0;
    }
    .panel-title {
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: #94a3b8;
    }
    .panel-count {
      font-size: 11px;
      color: #64748b;
    }

    .entries {
      flex: 1;
      overflow: auto;
      padding: 8px 0 12px;
    }

    .empty {
      padding: 20px 16px;
      color: #334155;
      font-size: 12px;
      font-style: italic;
    }

    .entry {
      margin: 0 10px 10px;
      border: 1px solid #1e293b;
      border-radius: 10px;
      background: #0b1220;
      overflow: hidden;
    }

    .entry-head {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px 10px;
      border-bottom: 1px solid #1e293b;
      background: #0f172a;
      min-width: 0;
    }
    .tool-name {
      font-size: 11px;
      font-weight: 600;
      color: #38bdf8;
      background: #082f49;
      border: 1px solid #0c4a6e;
      border-radius: 999px;
      padding: 2px 8px;
      white-space: nowrap;
    }
    .status-badge {
      margin-left: auto;
      font-size: 10px;
      font-weight: 600;
      padding: 1px 6px;
      border-radius: 999px;
      white-space: nowrap;
      flex-shrink: 0;
    }
    .badge-running { background: #1e3a5f; color: #60a5fa; }
    .badge-success { background: #14532d; color: #86efac; }
    .badge-error { background: #7f1d1d; color: #fca5a5; }
    .badge-skipped { background: #1e293b; color: #64748b; }
    .badge-timeout { background: #78350f; color: #fcd34d; }
    .badge-user-cancelled { background: #4c1d95; color: #ddd6fe; }

    .entry-body {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding: 10px;
    }
    .label {
      font-size: 11px;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: #64748b;
      margin-bottom: 3px;
    }
    .command {
      color: #e2e8f0;
      white-space: pre-wrap;
      word-break: break-word;
      font-family: ui-monospace, 'Cascadia Code', 'Fira Code', monospace;
      font-size: 12px;
      line-height: 1.5;
      margin: 0;
    }
    .output {
      color: #94a3b8;
      white-space: pre-wrap;
      word-break: break-word;
      font-family: ui-monospace, 'Cascadia Code', 'Fira Code', monospace;
      font-size: 12px;
      line-height: 1.5;
      margin: 0;
    }
    .output.error { color: #f87171; }
    .output.skipped { color: #64748b; font-style: italic; }
  `;Ks([fe({type:Array})],En.prototype,"entries",2);Ks([fe({type:Boolean})],En.prototype,"loading",2);En=Ks([Ze("bashgpt-tool-calls-panel")],En);var ol=Object.defineProperty,ll=Object.getOwnPropertyDescriptor,ds=(t,e,n,i)=>{for(var s=i>1?void 0:i?ll(e,n):e,a=t.length-1,r;a>=0;a--)(r=t[a])&&(s=(i?r(e,n,s):r(s))||s);return i&&s&&ol(e,n,s),s};let Vt=class extends Te{constructor(){super(...arguments),this.markdown="",this.loading=!1,this.tokenUsage=null}_renderStats(){const t=this.tokenUsage;if(!t||t.inputTokens===0&&t.outputTokens===0)return"";const e=t.totalTokens??t.inputTokens+t.outputTokens,n=t.cachedInputTokens??0;return T`
      <div class="stats-section">
        <div class="stats-title">Session Tokens</div>
        <div class="stats-grid">
          <div class="stat-item">
            <span class="stat-label">input</span>
            <span class="stat-value">${t.inputTokens.toLocaleString()}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">output</span>
            <span class="stat-value">${t.outputTokens.toLocaleString()}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">total</span>
            <span class="stat-value">${e.toLocaleString()}</span>
          </div>
          ${n>0?T`
            <div class="stat-item">
              <span class="stat-label">cached</span>
              <span class="stat-value">${n.toLocaleString()}</span>
            </div>
          `:""}
        </div>
      </div>
    `}render(){const t=T`
      <div class="panel-header">
        <div class="dot dot-red"></div>
        <div class="dot dot-yellow"></div>
        <div class="dot dot-green"></div>
        <div class="panel-title">Info</div>
      </div>
    `;return this.loading?T`${t}
        <div class="content">
          <div class="loading-state">
            <div class="spinner"></div>
            Loading information...
          </div>
        </div>
        ${this._renderStats()}`:this.markdown?T`${t}
      <div class="content">
        <div class="md">${Zi(F.parse(this.markdown))}</div>
      </div>
      ${this._renderStats()}`:T`${t}
        <div class="content">
          <div class="empty-state">No information available.</div>
        </div>
        ${this._renderStats()}`}};Vt.styles=Ke`
    :host {
      display: flex;
      flex-direction: column;
      background: #020617;
      border-left: 1px solid #1e293b;
      font-family: ui-monospace, 'Cascadia Code', 'Fira Code', monospace;
      font-size: 12px;
      overflow: hidden;
    }

    .panel-header {
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 8px 12px;
      border-bottom: 1px solid #1e293b;
      background: #0b1120;
      flex-shrink: 0;
    }
    .panel-title {
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: #475569;
      flex: 1;
    }
    .dot { width: 8px; height: 8px; border-radius: 50%; }
    .dot-red { background: #ef4444; }
    .dot-yellow { background: #f59e0b; }
    .dot-green { background: #22c55e; }

    .content {
      flex: 1;
      overflow-y: auto;
      padding: 16px;
    }

    .loading-state,
    .empty-state {
      padding: 20px 0;
      color: #334155;
      font-size: 12px;
      font-style: italic;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .spinner {
      width: 10px;
      height: 10px;
      border: 1.5px solid #1e3a5f;
      border-top-color: #60a5fa;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    .md h1 {
      font-size: 14px;
      font-weight: 700;
      color: #e2e8f0;
      margin: 0 0 12px;
      padding-bottom: 6px;
      border-bottom: 1px solid #1e293b;
    }
    .md h2 {
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: #475569;
      margin: 16px 0 6px;
    }
    .md p {
      color: #94a3b8;
      line-height: 1.6;
      margin: 0 0 8px;
    }
    .md ul {
      margin: 0 0 8px;
      padding-left: 16px;
    }
    .md li {
      color: #94a3b8;
      line-height: 1.6;
      margin-bottom: 2px;
    }
    .md code {
      color: #7dd3fc;
      background: #0f172a;
      padding: 1px 4px;
      border-radius: 3px;
    }
    .md table {
      width: 100%;
      border-collapse: collapse;
      margin-bottom: 12px;
    }
    .md th {
      font-size: 10px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: #334155;
      text-align: left;
      padding: 4px 6px;
      border-bottom: 1px solid #1e293b;
    }
    .md td {
      color: #94a3b8;
      padding: 4px 6px;
      border-bottom: 1px solid #0f172a;
      vertical-align: top;
    }
    .md td code {
      font-size: 11px;
    }
    .md tr:last-child td { border-bottom: none; }

    .stats-section {
      border-top: 1px solid #1e293b;
      padding: 10px 16px 14px;
      flex-shrink: 0;
    }
    .stats-title {
      font-size: 10px;
      font-weight: 600;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: #334155;
      margin-bottom: 6px;
    }
    .stats-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 4px 8px;
    }
    .stat-item {
      display: flex;
      justify-content: space-between;
      align-items: baseline;
      gap: 4px;
    }
    .stat-label {
      font-size: 10px;
      color: #334155;
      white-space: nowrap;
    }
    .stat-value {
      font-size: 11px;
      color: #7dd3fc;
      font-weight: 600;
    }
    .stat-value.zero {
      color: #334155;
    }
  `;ds([fe({type:String})],Vt.prototype,"markdown",2);ds([fe({type:Boolean})],Vt.prototype,"loading",2);ds([fe({attribute:!1})],Vt.prototype,"tokenUsage",2);Vt=ds([Ze("bashgpt-chat-info-panel")],Vt);var cl=Object.defineProperty,dl=Object.getOwnPropertyDescriptor,ue=(t,e,n,i)=>{for(var s=i>1?void 0:i?dl(e,n):e,a=t.length-1,r;a>=0;a--)(r=t[a])&&(s=(i?r(e,n,s):r(s))||s);return i&&s&&cl(e,n,s),s};let le=class extends Te{constructor(){super(...arguments),this.pendingPrompt="",this.active=!1,this.sessionId="",this.agentId="",this._chat={messages:[],loading:!1,statusText:"",statusError:!1,tokenUsage:{inputTokens:0,outputTokens:0,totalTokens:0}},this._panels={toolCallsOpen:!0,infoOpen:!1},this._infoPanel={markdown:"",loading:!1},this._idCounter=0,this._historyLoadSeq=0,this._lastHandledPendingPrompt="",this._streamingContent="",this._reasoningContent="",this._streamingId=null,this._newRoundPending=!1,this._streamingEntries=[],this._panelSizes={toolCalls:360,info:320},this._enabledTools=[],this._toolPickerOpen=!1,this._availableTools=[],this._activeRequestId=null,this._cancelRequested=!1,this._resizeState=null,this._layoutStorageKey="bashgpt_chat_layout_v1",this._handleWidth=6,this._minToolCallsWidth=240,this._minChatWidth=420,this._minInfoWidth=260,this._onResizeUp=()=>{this._savePanelSizes(),this._stopResize()},this._onResizeMove=t=>{const e=this._resizeState;if(!e)return;const n=this._panels.toolCallsOpen,i=this._panels.infoOpen,s=(n?1:0)+(i?1:0),a=e.containerWidth-s*this._handleWidth,r=t.clientX-e.startX;let d=e.startToolCalls,o=e.startInfo;if(e.type==="toolCalls"){const u=a-(i?o:0)-this._minChatWidth;d=this._clamp(e.startToolCalls+r,this._minToolCallsWidth,Math.max(this._minToolCallsWidth,u)),this._panelSizes={...this._panelSizes,toolCalls:Math.round(d)};return}const p=a-(n?d:0)-this._minChatWidth;o=this._clamp(e.startInfo-r,this._minInfoWidth,Math.max(this._minInfoWidth,p)),this._panelSizes={...this._panelSizes,info:Math.round(o)}}}async connectedCallback(){super.connectedCallback(),this._loadPanelSizes(),await this._loadHistory()}disconnectedCallback(){super.disconnectedCallback(),this._stopResize()}updated(t){t.has("pendingPrompt")&&this.pendingPrompt?this.pendingPrompt!==this._lastHandledPendingPrompt&&(this._lastHandledPendingPrompt=this.pendingPrompt,this._sendPrompt(this.pendingPrompt)):t.has("pendingPrompt")&&!this.pendingPrompt&&(this._lastHandledPendingPrompt=""),t.has("active")&&this.active&&this._chat.messages.length===0&&this._loadHistory(),t.has("agentId")&&this._panels.infoOpen&&this._loadInfoPanel()}loadSnapshot(t,e,n){this._historyLoadSeq++;const i=t.filter(s=>(s.role==="user"||s.role==="assistant")&&s.content.trim()!=="").map(s=>({id:this._idCounter++,role:s.role,content:s.content,commands:s.commands,usage:s.usage}));this._chat={...this._chat,messages:i,tokenUsage:this._sumTokenUsage(i),statusText:e??"",statusError:!1},this._enabledTools=n??[],this._toolPickerOpen=!1}scrollToBottom(){this.updateComplete.then(()=>{const t=this.shadowRoot?.querySelector("#chat");t&&(t.scrollTop=t.scrollHeight)})}getSnapshot(){return this._chat.messages.map(t=>({role:t.role,content:t.content,...t.commands?.length?{commands:t.commands}:{},...t.usage?{usage:t.usage}:{}}))}async reset(){try{this.sessionId||await qi(),this._chat={...this._chat,messages:[],tokenUsage:{inputTokens:0,outputTokens:0,totalTokens:0},statusText:"Verlauf gelöscht",statusError:!1},this._enabledTools=[],this._toolPickerOpen=!1,this._emitMessagesChanged()}catch(t){this._chat={...this._chat,statusText:`Fehler: ${t instanceof Error?t.message:String(t)}`,statusError:!0}}}async _loadHistory(){const t=++this._historyLoadSeq;try{const e=await ji();if(t!==this._historyLoadSeq||this._chat.messages.length>0)return;const n=e.map(i=>({id:this._idCounter++,role:i.role,content:i.content}));this._chat={...this._chat,messages:n,tokenUsage:this._sumTokenUsage(n),statusError:!1}}catch(e){this._chat={...this._chat,statusError:!0,statusText:`Fehler: ${e instanceof Error?e.message:String(e)}`}}}async _sendPrompt(t){if(!t.trim()||this._chat.loading)return;this._historyLoadSeq++,this._activeRequestId=this._createRequestId(),this._cancelRequested=!1;const e=this._idCounter++;this._streamingId=e,this._streamingContent="",this._reasoningContent="",this._newRoundPending=!1,this._streamingEntries=[],this._chat={...this._chat,messages:[...this._chat.messages,{id:this._idCounter++,role:"user",content:t},{id:e,role:"assistant",content:""}],loading:!0,statusText:"Denke…",statusError:!1},this.dispatchEvent(new CustomEvent("chat-started",{bubbles:!0,composed:!0}));try{this.beforeSend&&await this.beforeSend();const n=await ma(t,{onReasoningToken:o=>{this._newRoundPending?(this._reasoningContent=o,this._newRoundPending=!1):this._reasoningContent+=o},onToken:o=>{this._streamingContent+=o,this._chat={...this._chat,messages:this._chat.messages.map(p=>p.id===e?{...p,content:this._streamingContent}:p)}},onToolCall:o=>{this._chat={...this._chat,statusText:`Führe aus: ${o.command}`},this._streamingEntries=[...this._streamingEntries,{toolName:o.name||"tool",command:o.command,output:"",exitCode:-1,wasExecuted:!1,status:"running"}]},onCommandResult:o=>{let p=!1;this._streamingEntries=this._streamingEntries.map(u=>{if(!p&&u.command===o.command&&u.status==="running"){p=!0;const g=o.status??"",m=g==="timeout"||g==="user_cancelled"||g==="success"||g==="error"||g==="skipped"?g:o.wasExecuted?o.exitCode===0?"success":"error":"skipped";return{toolName:u.toolName,command:o.command,output:o.output??"",exitCode:o.exitCode,wasExecuted:o.wasExecuted,status:m}}return u}),this._chat={...this._chat,statusText:"Verarbeite Ergebnis…"}},onRoundStart:o=>{this._chat={...this._chat,statusText:`Tool-Runde ${o.round}…`},this._newRoundPending=!0}},this.sessionId||void 0,this._enabledTools.length?this._enabledTools:void 0,this.agentId||void 0,this._activeRequestId||void 0),i=this._streamingEntries.map(o=>({command:o.command,exitCode:o.status==="running"?-1:o.exitCode,output:o.output??"",wasExecuted:o.status==="running"?!1:o.wasExecuted})),s=(n.commands?.length??0)>0?n.commands:i,a=n.finalStatus==="user_cancelled"?this._streamingContent||n.response||"Vom Nutzer abgebrochen.":n.response,r=this._chat.messages.map(o=>o.id===e?{...o,content:a,usage:n.usage??void 0,commands:s}:o),d=n.finalStatus==="user_cancelled"?"Vom Nutzer abgebrochen":n.finalStatus==="timeout"?"Timeout":this._enabledTools.length?`Tools: ${this._enabledTools.join(", ")}`:"";this._chat={...this._chat,messages:r,tokenUsage:this._sumTokenUsage(r),statusText:d,statusError:!1},this._streamingContent="",this._reasoningContent="",this._streamingId=null,this._streamingEntries=[],this._emitMessagesChanged()}catch(n){const i=`Fehler: ${n instanceof Error?n.message:String(n)}`,s=this._chat.messages.map(a=>a.id===e?{...a,content:`⚠️ ${i}`}:a);this._chat={...this._chat,messages:s,statusText:i,statusError:!0},this._streamingContent="",this._reasoningContent="",this._streamingId=null,this._streamingEntries=[],this._emitMessagesChanged()}finally{this._activeRequestId=null,this._cancelRequested=!1,this._chat={...this._chat,loading:!1}}}async _send(){const t=this.shadowRoot.querySelector("textarea"),e=t.value.trim();e&&(t.value="",await this._sendPrompt(e))}_createRequestId(){const t=globalThis.crypto;return t&&typeof t.randomUUID=="function"?t.randomUUID():`req_${Date.now()}_${Math.random().toString(16).slice(2)}`}async _cancelRun(){if(!(!this._activeRequestId||this._cancelRequested)){this._cancelRequested=!0,this._chat={...this._chat,statusText:"Abbruch angefordert…",statusError:!1};try{await _a(this._activeRequestId)}catch(t){this._chat={...this._chat,statusText:`Fehler beim Abbrechen: ${t instanceof Error?t.message:String(t)}`,statusError:!0}}}}_onKeydown(t){t.key==="Enter"&&(t.metaKey||t.ctrlKey)&&(t.preventDefault(),this._send())}_emitMessagesChanged(){this.dispatchEvent(new CustomEvent("messages-changed",{bubbles:!0,composed:!0,detail:{messages:this.getSnapshot()}}))}_toolCallEntries(t){const e=[];for(const n of t)if(!(n.role!=="assistant"||!n.commands?.length))for(const i of n.commands)e.push({toolName:"shell_exec",command:i.command,output:i.output,exitCode:i.exitCode,wasExecuted:i.wasExecuted,status:this._commandStatus(i)});return e}_commandStatus(t){return t.wasExecuted?(t.output??"").toLowerCase().includes("timed out")?"timeout":t.exitCode===0?"success":"error":"skipped"}_loadPanelSizes(){try{const t=localStorage.getItem(this._layoutStorageKey);if(!t)return;const e=JSON.parse(t);typeof e.toolCalls=="number"&&Number.isFinite(e.toolCalls)&&(this._panelSizes.toolCalls=Math.round(e.toolCalls)),typeof e.info=="number"&&Number.isFinite(e.info)&&(this._panelSizes.info=Math.round(e.info))}catch{}}_savePanelSizes(){localStorage.setItem(this._layoutStorageKey,JSON.stringify(this._panelSizes))}_clamp(t,e,n){return Math.max(e,Math.min(n,t))}_startResize(t,e){e.preventDefault();const n=this.shadowRoot?.querySelector(".split-wrapper");if(!n)return;const i=n.getBoundingClientRect();this._resizeState={type:t,startX:e.clientX,startToolCalls:this._panelSizes.toolCalls,startInfo:this._panelSizes.info,containerWidth:i.width},window.addEventListener("pointermove",this._onResizeMove),window.addEventListener("pointerup",this._onResizeUp,{once:!0})}_stopResize(){this._resizeState=null,window.removeEventListener("pointermove",this._onResizeMove)}_resizeByKeyboard(t,e){const n=e.shiftKey?40:16,i=e.key==="ArrowLeft",s=e.key==="ArrowRight";if(!i&&!s)return;e.preventDefault();const a=s?1:-1,r=t==="toolCalls"?this._panelSizes.toolCalls:this._panelSizes.info,d=t==="toolCalls"?this._minToolCallsWidth:this._minInfoWidth,o=Math.max(d,r+a*n);t==="toolCalls"?this._panelSizes={...this._panelSizes,toolCalls:o}:this._panelSizes={...this._panelSizes,info:o},this._savePanelSizes()}async _toggleInfo(){const t=!this._panels.infoOpen;this._panels={...this._panels,infoOpen:t},t&&await this._loadInfoPanel()}async _toggleToolPicker(){this._toolPickerOpen=!this._toolPickerOpen,this._toolPickerOpen&&this._availableTools.length===0&&(this._availableTools=await Ki())}async _loadInfoPanel(){const t=this.agentId||"generic";this._infoPanel={markdown:"",loading:!0};try{const e=await Aa(t);this._infoPanel={markdown:e,loading:!1}}catch{this._infoPanel={markdown:"",loading:!1}}}_toggleTool(t){this._enabledTools=this._enabledTools.includes(t)?this._enabledTools.filter(e=>e!==t):[...this._enabledTools,t]}_sumTokenUsage(t){let e=0,n=0,i=0;for(const s of t)s.usage&&(e+=s.usage.inputTokens,n+=s.usage.outputTokens,i+=s.usage.cachedInputTokens??0);return{inputTokens:e,outputTokens:n,totalTokens:e+n,cachedInputTokens:i}}_workingText(){return this._chat.loading?this._chat.statusText||"Denke…":""}render(){const t=this._chat.messages.length===0,e=this._workingText(),n=this._panels.toolCallsOpen,i=this._panels.infoOpen,s=this._toolCallEntries(this._chat.messages),a=[...n?[`${this._panelSizes.toolCalls}px`,`${this._handleWidth}px`]:[],"minmax(0, 1fr)",...i?[`${this._handleWidth}px`,`${this._panelSizes.info}px`]:[]].join(" ");return T`
      <div class="split-wrapper" style=${`grid-template-columns: ${a};`}>
        ${n?T`
          <bashgpt-tool-calls-panel
            .entries=${[...s,...this._streamingEntries]}
            ?loading=${this._chat.loading}
          ></bashgpt-tool-calls-panel>

          <div
            class="resize-handle"
            role="separator"
            aria-label="Breite Tool-Calls anpassen"
            aria-orientation="vertical"
            tabindex="0"
            @pointerdown=${r=>this._startResize("toolCalls",r)}
            @keydown=${r=>this._resizeByKeyboard("toolCalls",r)}
          ></div>
        `:""}

        <div class="chat-column">
          <div id="chat">

            ${t?T`
                  <div class="empty-state">
                    <div class="icon">⌨️</div>
                    <p>Stell mir eine Frage oder wähle einen Use-Case.</p>
                  </div>
                `:vn(this._chat.messages,r=>r.id,r=>T`
                    <bashgpt-message
                      role=${r.role}
                      content=${r.id===this._streamingId&&this._reasoningContent&&!this._streamingContent?this._reasoningContent:r.content}
                      ?reasoning=${r.id===this._streamingId&&!!this._reasoningContent&&!this._streamingContent}
                    ></bashgpt-message>
                  `)}
          </div>

          ${this._chat.loading?T`
            <div class="working-bar">
              <div class="spinner"></div>
              ${e}
            </div>
          `:""}
        </div>

        ${i?T`
          <div
            class="resize-handle"
            role="separator"
            aria-label="Breite Info-Panel anpassen"
            aria-orientation="vertical"
            tabindex="0"
            @pointerdown=${r=>this._startResize("info",r)}
            @keydown=${r=>this._resizeByKeyboard("info",r)}
          ></div>
          <bashgpt-chat-info-panel
            .markdown=${this._infoPanel.markdown}
            ?loading=${this._infoPanel.loading}
            .tokenUsage=${this._chat.tokenUsage.inputTokens>0||this._chat.tokenUsage.outputTokens>0?this._chat.tokenUsage:null}
          ></bashgpt-chat-info-panel>
        `:""}
      </div>

      <footer>
        ${this._toolPickerOpen?T`
          <div class="tool-picker">
            <div class="tool-picker-title">🔧 Tools für diese Session</div>
            ${this._availableTools.length===0?T`<span style="font-size:12px;color:#64748b">Keine Tools verfügbar.</span>`:T`
                <div class="tool-picker-list">
                  ${this._availableTools.map(r=>T`
                    <button
                      class="tool-chip ${this._enabledTools.includes(r.name)?"active":""}"
                      @click=${()=>this._toggleTool(r.name)}
                      title=${r.description}
                    >${r.name}</button>
                  `)}
                </div>
              `}
          </div>
        `:""}

        <div class="input-row">
          <textarea
            placeholder="Nachricht eingeben… (Cmd+Enter zum Senden)"
            aria-label="Nachricht eingeben"
            @keydown=${this._onKeydown}
            ?disabled=${this._chat.loading}
          ></textarea>
        </div>
        <div class="controls">
          <button
            class="terminal-toggle ${this._toolPickerOpen||this._enabledTools.length>0?"active":""}"
            @click=${this._toggleToolPicker}
            title="Tools für diese Session konfigurieren"
            aria-pressed=${this._toolPickerOpen?"true":"false"}
          >🔧 Tools${this._enabledTools.length>0?` (${this._enabledTools.length})`:""}</button>

          <button
            class="terminal-toggle ${this._panels.toolCallsOpen?"active":""}"
            @click=${()=>{this._panels={...this._panels,toolCallsOpen:!this._panels.toolCallsOpen}}}
            title="Tool-Calls ein-/ausblenden"
            aria-pressed=${this._panels.toolCallsOpen?"true":"false"}
            aria-label="Tool-Calls ein-/ausblenden"
          >Tool Calls</button>

          <button
            class="terminal-toggle ${this._panels.infoOpen?"active":""}"
            @click=${this._toggleInfo}
            title="Info-Panel ein-/ausblenden"
            aria-pressed=${this._panels.infoOpen?"true":"false"}
          >ℹ Info</button>

          <span
            class="status ${this._chat.statusError?"error":""}"
            aria-live="polite"
            aria-atomic="true"
          >
            ${this._chat.statusText}
          </span>

          ${this._chat.loading?T`
            <button
              class="cancel"
              @click=${this._cancelRun}
              ?disabled=${this._cancelRequested}
              aria-label="Laufenden Tool-Call abbrechen"
            >
              ${this._cancelRequested?"Abbrechen…":"Abbrechen"}
            </button>
          `:""}

          <button
            class="primary"
            @click=${this._send}
            ?disabled=${this._chat.loading}
            aria-label="Nachricht senden"
          >
            Senden
          </button>
        </div>
      </footer>
    `}};le.styles=Ke`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow: hidden;
    }

    /* ── Split layout ───────────────────────────────────────────────────── */
    .split-wrapper {
      flex: 1;
      display: grid;
      overflow: hidden;
      align-items: stretch;
    }

    bashgpt-tool-calls-panel {
      min-width: 0;
      height: 100%;
    }

    .chat-column {
      min-width: 0;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    .resize-handle {
      width: 6px;
      cursor: col-resize;
      background: #0f172a;
      border-left: 1px solid #1e293b;
      border-right: 1px solid #1e293b;
      transition: background 0.15s;
      user-select: none;
      touch-action: none;
    }
    .resize-handle:hover,
    .resize-handle:focus-visible {
      background: #1e293b;
      outline: none;
    }

    /* ── Chat area ──────────────────────────────────────────────────────── */
    #chat {
      flex: 1;
      overflow-y: auto;
      padding: 16px 20px;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .empty-state {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 12px;
      color: #475569;
    }
    .empty-state .icon { font-size: 40px; }
    .empty-state p { font-size: 15px; }

    /* ── Working indicator bar ──────────────────────────────────────────── */
    .working-bar {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 6px 20px;
      background: #0b1a2e;
      border-top: 1px solid #1e3a5f;
      font-size: 12px;
      color: #60a5fa;
      flex-shrink: 0;
    }
    .working-bar .spinner {
      width: 12px; height: 12px;
      border: 1.5px solid #1e3a5f;
      border-top-color: #60a5fa;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    /* ── Footer ─────────────────────────────────────────────────────────── */
    footer {
      padding: 10px 16px 16px;
      border-top: 1px solid #1e293b;
      background: rgba(15, 23, 42, 0.8);
      backdrop-filter: blur(8px);
      flex-shrink: 0;
    }

    .input-row { display: flex; gap: 8px; align-items: flex-end; }

    textarea {
      flex: 1;
      min-height: 52px;
      max-height: 160px;
      resize: vertical;
      background: #111827;
      color: #e5e7eb;
      border: 1px solid #374151;
      border-radius: 10px;
      padding: 10px 14px;
      font-family: inherit;
      font-size: 14px;
      line-height: 1.5;
      outline: none;
      transition: border-color 0.15s;
    }
    textarea:focus { border-color: #4b5563; }
    textarea:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }
    textarea::placeholder { color: #4b5563; }
    textarea:disabled { opacity: 0.5; }

    .controls {
      display: flex;
      gap: 8px;
      margin-top: 8px;
      align-items: center;
      flex-wrap: wrap;
    }

    button {
      background: #111827;
      color: #e5e7eb;
      border: 1px solid #374151;
      border-radius: 8px;
      padding: 7px 14px;
      font-size: 13px;
      cursor: pointer;
      transition: background 0.15s, border-color 0.15s;
      font-family: inherit;
    }
    button:hover:not(:disabled) { background: #1f2937; border-color: #4b5563; }
    button:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }
    button:disabled { opacity: 0.4; cursor: not-allowed; }

    button.primary {
      background: #14532d;
      border-color: #16a34a;
      color: #dcfce7;
      font-weight: 600;
      padding: 7px 20px;
    }
    button.primary:hover:not(:disabled) { background: #166534; }
    button.cancel {
      background: #3f1d1d;
      border-color: #7f1d1d;
      color: #fecaca;
      font-weight: 600;
    }
    button.cancel:hover:not(:disabled) { background: #5f1d1d; }

    button.terminal-toggle {
      padding: 7px 10px;
      font-size: 12px;
      color: #64748b;
    }
    button.terminal-toggle.active { color: #22c55e; border-color: #166534; }

    .status {
      font-size: 12px;
      color: #4b5563;
      flex: 1;
      text-align: right;
    }
    .status.error { color: #ef4444; }

    /* ── Info Panel ─────────────────────────────────────────────────────── */
    bashgpt-chat-info-panel {
      min-width: 0;
      height: 100%;
      overflow: hidden;
      border-left: 1px solid #1e293b;
    }

    /* ── Tool-Picker ────────────────────────────────────────────────────── */
    .tool-picker {
      background: #0d1b2e;
      border: 1px solid #1e3a5f;
      border-radius: 10px;
      padding: 10px 12px;
      margin-bottom: 8px;
      display: flex;
      flex-direction: column;
      gap: 6px;
    }
    .tool-picker-title {
      font-size: 11px;
      color: #60a5fa;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .tool-picker-list {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
    }
    .tool-chip {
      display: flex;
      align-items: center;
      gap: 5px;
      padding: 4px 10px;
      border-radius: 20px;
      font-size: 12px;
      border: 1px solid #1e3a5f;
      background: #111827;
      color: #94a3b8;
      cursor: pointer;
      transition: background 0.15s, border-color 0.15s, color 0.15s;
    }
    .tool-chip:hover { background: #1e293b; }
    .tool-chip.active { background: #14532d; border-color: #16a34a; color: #dcfce7; }
    .tool-chip.active::before { content: '✓ '; }

    /* ── Mobile ─────────────────────────────────────────────────────────── */
    @media (max-width: 768px) {
      .split-wrapper { display: flex; flex-direction: column; }
      .resize-handle { display: none; }
      bashgpt-tool-calls-panel { height: auto; border-right: none; border-bottom: 1px solid #1e293b; }
      bashgpt-chat-info-panel {
        border-left: none;
        border-top: 1px solid #1e293b;
      }
    }
  `;ue([fe()],le.prototype,"pendingPrompt",2);ue([fe({type:Boolean})],le.prototype,"active",2);ue([fe({attribute:!1})],le.prototype,"beforeSend",2);ue([fe()],le.prototype,"sessionId",2);ue([fe()],le.prototype,"agentId",2);ue([W()],le.prototype,"_chat",2);ue([W()],le.prototype,"_panels",2);ue([W()],le.prototype,"_infoPanel",2);ue([W()],le.prototype,"_streamingContent",2);ue([W()],le.prototype,"_reasoningContent",2);ue([W()],le.prototype,"_streamingId",2);ue([W()],le.prototype,"_streamingEntries",2);ue([W()],le.prototype,"_panelSizes",2);ue([W()],le.prototype,"_enabledTools",2);ue([W()],le.prototype,"_toolPickerOpen",2);ue([W()],le.prototype,"_availableTools",2);ue([W()],le.prototype,"_cancelRequested",2);le=ue([Ze("bashgpt-chat-view")],le);const we="current",$r="bashgpt_sessions_v2",pl=20;function ul(t){return t.map(e=>({role:e.role,content:e.content}))}function Cs(t){const e=t.find(n=>n.role==="user")?.content?.trim();return e?e.length>40?`${e.slice(0,40)}…`:e:"Neuer Chat"}function hl(t){const e=new Date().toISOString();return{id:we,title:Cs(t),createdAt:e,updatedAt:e,messages:t}}function hn(t){return{id:t.id,title:t.title,createdAt:t.createdAt,updatedAt:t.updatedAt}}function Ni(){try{const t=localStorage.getItem($r);if(!t)return[];const e=JSON.parse(t);return Array.isArray(e)?e.filter(n=>n&&typeof n.id=="string"&&Array.isArray(n.messages)):[]}catch{return[]}}function pt(t){localStorage.setItem($r,JSON.stringify(t))}function Wn(t,e,n){const i=new Date().toISOString(),s=t.findIndex(a=>a.id===e);if(s>=0){const a=t[s];t[s]={...a,title:Cs(n),updatedAt:i,messages:n}}else t.unshift({id:e,title:Cs(n),createdAt:i,updatedAt:i,messages:n});return t.sort((a,r)=>{const d=r.updatedAt.localeCompare(a.updatedAt);return d!==0?d:r.createdAt.localeCompare(a.createdAt)}).slice(0,pl)}const Ii="s-";class gl{constructor(){this._localSessions=[],this._useFallback=!1}get useFallback(){return this._useFallback}get localSessions(){return this._localSessions}async init(){const e=await Ft();if(this._useFallback=e===null,!this._useFallback){await this._migrateLocalSessionsToServer();const i=await Ft()??[];if(i.length===0){const s=await fi();return{sessions:await Ft()??[],activeId:s?.id??null}}return{sessions:i,activeId:i[0].id}}if(this._localSessions=Ni(),!this._localSessions.some(i=>i.id===we))try{const i=await ji();i.length>0&&this._localSessions.unshift(hl(ul(i)))}catch{}pt(this._localSessions);const n=this._localSessions.map(hn);return{sessions:n,activeId:n[0]?.id??null}}async loadSession(e){if(this._useFallback){const i=this._localSessions.find(s=>s.id===e);return i?{messages:i.messages,isArchived:e!==we}:null}const n=await xa(e);return n?{messages:n.messages??[],enabledTools:n.enabledTools??void 0,agentId:n.agentId??null,isArchived:!1}:null}async prepareNewChat(e,n){if(this._useFallback){if(e.length>0&&n===we){const s=`${Ii}${Date.now()}`;this._localSessions=Wn(this._localSessions,s,e)}return this._localSessions=Wn(this._localSessions,we,[]),pt(this._localSessions),{sessions:this._localSessions.map(hn),activeId:we}}(!e||e.length===0)&&n&&await wa(n);const i=await fi();return{sessions:await Ft()??[],activeId:i?.id??null}}async persistMessages(e,n){return this._useFallback?(this._localSessions=Wn(this._localSessions,e,n),pt(this._localSessions),this._localSessions.map(hn)):await Ft()??[]}async activateArchived(e){const n=this._localSessions.find(a=>a.id===we);let i=this._localSessions.filter(a=>a.id!==we);n&&n.messages.length>0&&(i=[...i,{...n,id:`${Ii}${Date.now()}`}]);const s=new Date().toISOString();return this._localSessions=i.map(a=>a.id===e?{...a,id:we,updatedAt:s}:a).sort((a,r)=>r.updatedAt.localeCompare(a.updatedAt)),pt(this._localSessions),{sessions:this._localSessions.map(hn),activeId:we}}async clearAll(){this._useFallback?(this._localSessions=[],pt([])):await Ea()}ensureLiveSession(){return this._useFallback?(this._localSessions.some(e=>e.id===we)||(this._localSessions=Wn(this._localSessions,we,[])),pt(this._localSessions),this._localSessions.map(hn)):[]}async _migrateLocalSessionsToServer(){const e=Ni();if(e.length===0)return;if((await Ft()??[]).length>0){pt([]);return}for(const i of e)await ya(i.id,{title:i.title,messages:i.messages,createdAt:i.createdAt});pt([])}}const fl=Ke`
  /* ── CSS custom properties (cascade to all child components) ─────────── */
  :host {
    display: flex;
    flex-direction: column;
    height: 100dvh;
    font-family: ui-sans-serif, system-ui, sans-serif;
    background: radial-gradient(circle at top, #1e293b, #020617);
    color: #e5e7eb;
    --color-border: #374151;
    --color-user: #1f2937;
    --color-assistant: #0b1220;
    --color-text: #e5e7eb;
    --color-muted: #6b7280;
    --color-accent: #22c55e;
    --sidebar-width: 220px;
  }

  /* ── Shared header ───────────────────────────────────────────────────── */
  header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 12px 20px;
    border-bottom: 1px solid #1e293b;
    background: rgba(15, 23, 42, 0.9);
    backdrop-filter: blur(8px);
    flex-shrink: 0;
    z-index: 10;
  }
  .logo {
    font-size: 18px;
    font-weight: 700;
    color: #f1f5f9;
    display: flex;
    align-items: center;
    gap: 8px;
    cursor: pointer;
    user-select: none;
  }
  .logo-dot { color: var(--color-accent); }
  .header-actions { display: flex; gap: 8px; align-items: center; }

  button {
    background: #111827;
    color: #e5e7eb;
    border: 1px solid #374151;
    border-radius: 8px;
    padding: 7px 14px;
    font-size: 13px;
    cursor: pointer;
    transition: background 0.15s, border-color 0.15s;
    font-family: inherit;
  }
  button:hover:not(:disabled) { background: #1f2937; border-color: #4b5563; }
  button:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }
  button:disabled { opacity: 0.4; cursor: not-allowed; }
  button.primary {
    background: #14532d;
    border-color: #16a34a;
    color: #dcfce7;
    font-weight: 600;
    padding: 7px 20px;
  }
  button.primary:hover:not(:disabled) { background: #166534; }

  /* ── Shell layout ─────────────────────────────────────────────────────── */
  .shell {
    display: flex;
    flex: 1;
    overflow: hidden;
  }
  .content {
    flex: 1;
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }

  /* ── Mobile: sidebar overlay ─────────────────────────────────────────── */
  .mobile-overlay {
    display: none;
  }
  @media (max-width: 768px) {
    .mobile-overlay {
      display: block;
      position: fixed;
      inset: 0;
      background: rgba(0,0,0,0.5);
      z-index: 20;
    }
    bashgpt-sidebar {
      position: fixed;
      top: 0;
      left: 0;
      bottom: 0;
      z-index: 30;
      transform: translateX(-100%);
      transition: transform 0.2s ease;
      width: 260px !important;
    }
    bashgpt-sidebar.open {
      transform: translateX(0);
    }
    .hamburger { display: flex !important; }
  }
  .hamburger {
    display: none;
    background: none;
    border: none;
    color: #94a3b8;
    font-size: 20px;
    padding: 4px 8px;
    cursor: pointer;
  }
`;var bl=Object.defineProperty,ml=Object.getOwnPropertyDescriptor,Nt=(t,e,n,i)=>{for(var s=i>1?void 0:i?ml(e,n):e,a=t.length-1,r;a>=0;a--)(r=t[a])&&(s=(i?r(e,n,s):r(s))||s);return i&&s&&bl(e,n,s),s};let et=class extends Te{constructor(){super(...arguments),this._view="dashboard",this._sessions=[],this._activeSessionId=null,this._pendingPrompt="",this._mobileMenuOpen=!1,this._activeAgentId="",this._sm=new gl}async connectedCallback(){super.connectedCallback();const{sessions:t,activeId:e}=await this._sm.init();this._sessions=t,this._activeSessionId=e,await this.updateComplete,e&&await this._loadSessionIntoView(e)}async _loadSessionIntoView(t){const e=await this._sm.loadSession(t);if(!e)return;const n=this.shadowRoot?.querySelector("bashgpt-chat-view");if(n)if(e.isArchived){const i=t;n.beforeSend=async()=>{n.beforeSend=void 0,await this._doActivateArchived(i)},n.loadSnapshot?.(e.messages,"Archivierte Session – Nachricht senden, um fortzufahren",e.enabledTools),n.scrollToBottom?.()}else n.beforeSend=void 0,this._activeAgentId=e.agentId??"",n.loadSnapshot?.(e.messages,void 0,e.enabledTools),n.scrollToBottom?.()}async _doActivateArchived(t){await qi();const{sessions:e,activeId:n}=await this._sm.activateArchived(t);this._sessions=e,this._activeSessionId=n}_ensureLiveSessionActive(){if(this._sm.useFallback){this._activeSessionId=we;const t=this._sm.ensureLiveSession();t.length>0&&(this._sessions=t)}}async _onNewChat(){const t=this.shadowRoot?.querySelector("bashgpt-chat-view");t&&(t.beforeSend=void 0);const e=t?.getSnapshot?.()??[],{sessions:n,activeId:i}=await this._sm.prepareNewChat(e,this._activeSessionId);this._sessions=n,this._activeSessionId=i,t&&await t.reset(),this._pendingPrompt="",this._activeAgentId="",this._view="chat",this._mobileMenuOpen=!1}_onViewChange(t){this._view=t.detail.view,this._mobileMenuOpen=!1}async _onSessionSelect(t){this._activeSessionId=t.detail.id,this._activeAgentId="",this._view="chat",this._mobileMenuOpen=!1,await this._loadSessionIntoView(t.detail.id)}async _onPromptSelected(t){await this._onNewChat(),this._ensureLiveSessionActive(),this._view="chat",this._pendingPrompt="",requestAnimationFrame(()=>{this._pendingPrompt=t.detail.prompt})}_onPromptEdit(t){this._ensureLiveSessionActive(),this._pendingPrompt="",this._view="chat",requestAnimationFrame(()=>{const n=this.shadowRoot?.querySelector("bashgpt-chat-view")?.shadowRoot?.querySelector("textarea");n&&(n.value=t.detail.prompt,n.focus())})}async _onClearHistory(){const t=this.shadowRoot?.querySelector("bashgpt-chat-view");t&&(t.beforeSend=void 0,await t.reset()),await this._sm.clearAll(),this._sessions=[],this._activeSessionId=null}_onChatStarted(){const t=this._activeSessionId;if(t){if(!this._sessions.some(e=>e.id===t)){const e=new Date().toISOString();this._sessions=[{id:t,title:"Aktueller Chat",createdAt:e,updatedAt:e},...this._sessions]}this._sm.useFallback&&(this._activeSessionId=we)}}async _onAgentChatStart(t){await this._onNewChat(),this._activeAgentId=t.detail.agentId,this._view="chat",this._mobileMenuOpen=!1}async _onMessagesChanged(t){const e=this._activeSessionId??we;this._activeSessionId||(this._activeSessionId=e),this._sessions=await this._sm.persistMessages(e,t.detail.messages)}render(){return T`
      <header>
        <div class="logo" @click=${()=>{this._view="dashboard",this._mobileMenuOpen=!1}}>
          <span class="logo-dot">●</span> bashGPT
        </div>
        <div class="header-actions">
          <button
            class="hamburger"
            @click=${()=>{this._mobileMenuOpen=!this._mobileMenuOpen}}
            aria-label="Menü"
          >☰</button>
        </div>
      </header>

      ${this._mobileMenuOpen?T`<div class="mobile-overlay" @click=${()=>{this._mobileMenuOpen=!1}}></div>`:""}

      <div class="shell">
        <bashgpt-sidebar
          class="${this._mobileMenuOpen?"open":""}"
          view=${this._view}
          .sessions=${this._sessions}
          activeSessionId=${this._activeSessionId??""}
          @new-chat=${this._onNewChat}
          @view-change=${this._onViewChange}
          @session-select=${this._onSessionSelect}
        ></bashgpt-sidebar>

        <div class="content">
          ${this._view==="dashboard"?T`
            <bashgpt-dashboard
              @prompt-selected=${this._onPromptSelected}
              @prompt-edit=${this._onPromptEdit}
            ></bashgpt-dashboard>
          `:""}

          ${this._view==="settings"?T`
            <bashgpt-settings-view
              @clear-history=${this._onClearHistory}
            ></bashgpt-settings-view>
          `:""}

          ${this._view==="agents"?T`
            <bashgpt-agents-view @agent-chat-start=${this._onAgentChatStart}></bashgpt-agents-view>
          `:""}

          ${this._view==="tools"?T`
            <bashgpt-tools-view></bashgpt-tools-view>
          `:""}

          <bashgpt-chat-view
            style="display: ${this._view==="chat"?"flex":"none"}; flex-direction: column; height: 100%;"
            pendingPrompt=${this._pendingPrompt}
            sessionId=${this._activeSessionId??""}
            agentId=${this._activeAgentId}
            ?active=${this._view==="chat"}
            @chat-started=${this._onChatStarted}
            @messages-changed=${this._onMessagesChanged}
          ></bashgpt-chat-view>
        </div>
      </div>
    `}};et.styles=fl;Nt([W()],et.prototype,"_view",2);Nt([W()],et.prototype,"_sessions",2);Nt([W()],et.prototype,"_activeSessionId",2);Nt([W()],et.prototype,"_pendingPrompt",2);Nt([W()],et.prototype,"_mobileMenuOpen",2);Nt([W()],et.prototype,"_activeAgentId",2);et=Nt([Ze("bashgpt-app")],et);
