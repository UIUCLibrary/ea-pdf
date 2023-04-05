using Microsoft.VisualStudio.TestTools.UnitTesting;
using NDepend.Path;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIUCLibrary.EaPdf;
using iTextSharp.text.pdf;
using System.IO;
using ExCSS;
using NUglify;
using System.Threading;
using AngleSharp;
using AngleSharp.Css.Parser;
using AngleSharp.Css.Dom;
using System.Diagnostics;
using NUglify.Css;
using Fizzler;

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestCssProcessors
    {

        private readonly string badCss = @"
                h3 {color: yellow;
                @media print {
                    h3 {color: black; }
                    }
                ";

        private readonly string goodCss = @"
                h3 {color: yellow;}
                @media print {
                    h3 {color: black; }
                    }
                ";

        private readonly string bigCss = @"
 div[class=""gmailwidthfix""] {
     width: 100% !important;
}
 ;
/* NEXT STYLE */
 .ReadMsgBody {
     width: 100%;
     background-color: #ebebeb;
}
.ExternalClass {
     width: 100%;
     background-color: #ebebeb;
}
.ExternalClass, .ExternalClass span, .ExternalClass font, .ExternalClass td, .ExternalClass div {
     line-height: 100%;
}
body {
     -webkit-text-size-adjust: none;
     -ms-text-size-adjust: none;
}
body, table, tr, td {
     font-family: Garamond, serif;
     font-size: 13px;
     line-height: 21px;
}
html, body {
     margin: 0;
     padding: 0;
     background-color: #efefef;
     width: 100%;
}
p {
     font-family: Helvetica, Arial, sans-serif;
     color: #000000;
     margin: 0;
}
table[class*=""col ad""] td, .col.ad td{
     font-family: Helvetica, Arial, sans-serif;
     font-size:12px;
}
table.art-col.ad.ad_content_300 img {
     width: 100%;
     float: none;
}
table.art-col.ad.ad_content_180.col-2 img {
     width: 180px !important;
     float: none;
     margin: 0 60px;
}
table.art-col.ad img {
     margin: 0 5px 0 0;
}
table.art-col.ad.ad_content_90 img {
     width: 90px !important;
     float: left;
}
table.ad-container table.art-col.ad.ad_content_180 img {
     float: left;
     width: 180px !important;
}
table.ad-container table.art-col.ad.ad_content_180.col-2 img {
     margin: 0 60px;
}
table.ad-container table.art-col.ad.ad_content_300 img {
     float: left;
     width: 300px !important;
}
table.ad-container table.art-col.ad.ad_content_468 img {
     float: none;
     margin: 0 auto 5px auto;
     display: block;
}
table.ad-container table.art-col.ad.ad_content_180.no-text table,table.ad-container table.art-col.ad.ad_content_300.no-text table,table.ad-container table.art-col.ad.ad_content_468.no-text table {
     width: 100%;
}
table.ad-container table.art-col.ad.ad_content_180.no-text img,table.ad-container table.art-col.ad.ad_content_300.no-text img,table.ad-container table.art-col.ad.ad_content_468.no-text img {
     display: block;
     float: none;
     margin: 0 auto;
}
table.ad-container table.art-col.ad.ad_content_180.no-text h3,table.ad-container table.art-col.ad.ad_content_300.no-text h3,table.ad-container table.art-col.ad.ad_content_468.no-text h3 {
     min-width: 334px;
}
table.columns-container,table.ad-container {
     border-right: 1px dotted #d7d7d7;
}
table.ad-container.template-top {
     border-left: none;
     border-right: none;
}
table {
     border-spacing: 0;
     margin: 0 auto;
}
.container table {
     table-layout: fixed;
}
table td {
     border-collapse: separate;
}
.grid-table, .container {
     width: 100% !important;
     max-width: 670px;
}
.main-table-nobkgd {
     width: 670px;
     align: center;
     margin: 0 auto;
}
.main-table-nobkgd tr {
     text-align: center;
}
.yshortcuts a {
     border-bottom: none !important;
}
.header {
     width: 100%;
     max-width: 670px;
     height: 80px;
}
.header h1 {
     font-weight: bold;
}
.columns-container, .ad-container {
     border-top: 2px solid #d7d7d7;
     border-bottom: 2px solid #d7d7d7;
     width: 100%;
}
.ad-container.template-top {
     border-top: none !important;
     border-bottom: none !important;
     margin-bottom: 8px;
}
.ad img {
     margin-left: 0;
}
h1 {
     margin: 5px 0 5px 15px;
     color: #ffffff;
     text-transform: uppercase;
     font-size: 26px;
     line-height: 28px;
     padding-right: 3px;
}
h1 font {
}
h1 span {
     font-weight: 100;
}
h5, .ad-tagline, .footer .footer p.footer-info {
     font-size: 12px;
}
h5 {
     margin: 0 0 0 15px;
     color: #ffffff;
     font-weight: 100;
}
h6 {
     margin: 0 0 0 20px;
     color: #999999;
     font-size: 10px;
}
a, a:visited {
     text-decoration: none;
     color: #004375;
}
a:hover {
     text-decoration: none;
     color: #999999;
}
.footer .footer a, .footer .footer a:visited {
     text-decoration: none;
     color: #222222;
}
.btn {
     display: block;
     text-align: center;
}
.btn-dark {
     display: block;
     text-align: center;
}
.btn, .btn-dark, .btn span, .btn-dark span {
     text-decoration: none;
     color: #ffffff;
     text-transform: uppercase;
     font-size: 14px;
     font-weight: bold;
     font-family: Helvetica, Arial, sans-serif;
}
.grid-list {
     list-style: none;
     margin: 10px 0 0 -20px;
     font-size: 15px;
}
.grid-list li, table[class*=""featured-links""] td {
     padding-bottom: 11px;
}
.nav {
     height: 40px;
     background-color: #b0b1b5;
     margin-bottom: 3px;
     box-shadow: 0 4px 0 #999999;
}
.nav li:first-child {
     padding: 0 35px 0 10px;
}
.nav li:last-child {
     border-right: none;
}
.nav li {
     font-size: 13px;
     font-weight: bold;
     display: block;
     float: left;
     padding: 0 30px 0 35px;
     margin-top: 12px;
     border-right: 1px solid #cccccc;
}
.nav li a {
     color: #ffffff;
}
.nav li a:hover {
     color: #666666;
}
/* This ribbon is based on a 16px font side and a 24px vertical rhythm. I've used em's to position each element for scalability. If you want to use a different font size you may have to play with the position of the ribbon elements */
.nav-ribbon {
     width: 690px;
     position: relative;
     background: transparent;
     color: #ffffff;
     text-align: center;
     padding: 0 0;
    /* Adjust to suit */
}
.nav-ribbon:before, .nav-ribbon:after {
     content: """";
     position: absolute;
     display: block;
     bottom: -.8em;
     z-index: -1;
}
.nav-ribbon:before {
     left: -2em;
     border-right-width: 1em;
     border-left-color: transparent;
}
.nav-ribbon:after {
     right: -2em;
     border-left-width: 1.5em;
     border-right-color: transparent;
}
.nav-ribbon .nav:before, .nav-ribbon .nav:after {
     content: """";
     position: absolute;
     display: block;
     border-style: solid;
     border-color: #999999 transparent transparent transparent;
     bottom: -.8em;
}
.nav-ribbon .nav:before {
     left: 0;
     border-width: .8em 0 0 .8em;
}
.nav-ribbon .nav:after {
     right: 0;
     border-width: .8em .8em 0 0;
}
table[class*=""art-col""] {
     padding: 0 !important;
}
table[class*=""featured""] {
     padding: 0 !important;
}
table[class=""eyebrow-wrap""] {
     margin-bottom: 10px;
     height: 23px;
     padding: 0 !important;
}
table[class*=""featured""] h3[class=""eyebrow""] {
     text-transform: uppercase;
     height: 20px;
}
table[class*=""col-2""] h3[class=""eyebrow""]{
     font-weight: bold;
     margin: 0 0 0 16px;
}
table[class*=""col-3""] h3[class=""eyebrow""] {
     font-weight: bold;
     margin: 0 0 0 11px;
}
.col-1 .eyebrow, table[class*=""col-1""] h3[class=""eyebrow""] {
     font-weight: bold;
     margin: 0 0 0 20px;
}
h2[class=""title""] {
     margin-top: 0;
     font-weight: bold;
}
table[class*=""featured""] h2[class=""title""] a:hover {
     color: #999999;
}
table[class*=""featured""] div[class=""body""] a {
     color: inherit;
}
table[class*=""featured-links""] {
     font-size: 16px;
     font-weight: bold;
     border: none;
}
table[class*=""featured-links""] td {
     margin-bottom: 3px;
}
table[class*=""col-1""] img[class=""90x110""], table[class*=""featured""] img[class=""90x110""], table[class*=""featured""] img[class=""featured_lg""], table[class*=""col-1""] img[class=""featured_lg""] {
     float: left;
     border: 1px solid #cccccc;
     background-color: #ffffff;
}
img[class=""featured_lg""] {
     margin: 0 15px 20px 5px;
     border: 1px solid #CCC;
}
img[class=""featured_sm""] {
     margin: 0 auto 10px auto;
     border: 1px solid #CCC;
     display: block;
}
img[class=""90x110""] {
     float: left;
     border: 1px solid #CCC;
}
table[class*=""col-1""] img[class=""90x110""] {
     margin: 0 15px 10px 5px;
}
table[class*=""col-2""] img[class=""90x110""] {
     margin: 0 10px 0 15px;
}
table[class*=""col-3""] img[class=""90x110""] {
     margin: 0 10px 0 10px;
}
img[class=""650x371""] {
     margin: 15px 0 0 0;
     width: 100%;
}
h3 + img[class=""featured_lg""], h3 + img[class=""featured_sm""] {
     margin-top: 5px;
}
.p-grid {
     width: 180px;
     margin-left: 22px;
     font-size: 13px;
     line-height: 18px;
     font-family: Helvetica, Arial, sans-serif;
}
.title-grid {
     margin: 10px 0 -5px 20px;
     font-size: 18px;
     font-weight: bold;
     width: 180px;
}
td[class*=""force-col""], .force-col {
     display: inline-block;
     *display: inline !important;
}
table[class*=""col-1""] {
     padding-left: 0;
}
table[class*=""col-2""] p[class=""byline""], table[class*=""col-3""] p[class=""byline""] {
     padding-left: 5%;
}
table[class*=""col-2""] p[class=""source""], table[class*=""col-3""] p[class=""source""] {
     padding-left: 5%;
}
table[class=""columns-container""] table[class*=""ad_content_670""] img {
     width: 100%;
}
*[class=""col last col-1""] img[class=""featured_lg""] {
     margin-right: 15px;
     margin-bottom: 15px;
     float: left;
}
.footer-branding {
     height: 55px;
     margin-bottom: 10px;
     background-color: #efefef;
     font-family: Helvetica, Arial, sans-serif;
}
.footer-branding tr td img {
     margin: 0 auto 0;
}
.footer .footer {
     background-color: #e5e5e5;
     padding: 20px;
     margin-top: 10px;
     font-family: Helvetica, Arial, sans-serif;
}
.footer .footer p {
     color: #222222;
}
p.footer-info span {
     font-weight: bold;
}
p.footer-info {
     font-size: 12px;
     font-family: Helvetica, Arial, sans-serif;
}
p.footer-info a {
     text-decoration: underline !important;
}
.footer .footer p.footer-links {
     font-size: 15px;
     text-align: center;
}
table[class=""columns-container""], table[class=""ad-container""] {
     background-color: #ffffff;
}
.footer .footer a, .footer .footer a:visited {
     text-decoration: none;
     color: #222222;
}
.footer .footer a:hover {
     text-decoration: none;
     color: #666666;
}
#powered-by-td{
     border-top: 1px solid #bfbfc1;
     border-bottom: 1px solid #444;
     background-color: #999;
     height: 35px;
}
#powered-by-img{
     float: left;
     width: 150px;
     padding-left: 18px;
}
td[class*=""force-col""], .force-col {
     min-height: 73px;
     background-color: #ffffff;
}
table[class*=""col ""] img {
     max-width: 100%;
     height: auto;
}
table[class*=""col-3""] img[class=""featured_lg""], table[class*=""col-2""] img[class=""featured_lg""] {
     float: none !important;
     margin-left: auto !important;
     margin-right: auto !important;
     display: block;
     padding: 0;
}
table[class*=""col-3""] h2[class=""title""], table[class*=""col-2""] h2[class=""title""], table[class*=""col-3""] div[class=""body""], table[class*=""col-2""] div[class=""body""], table[class*=""col-3""] img[class=""featured_lg""], table[class*=""col-2""] img[class=""featured_lg""] {
     margin-left: auto;
     margin-right: auto;
     width: 90%;
     display: block;
     padding: 0;
}
.force-col{
     border-left: 1px dotted #d7d7d7;
}
@media screen and (min-width: 681px) {
     .art-col {
         display: initial;
    }
     .art-col.no-text.col-count-1 h3, .art-col.no-text.col-count-1 a {
         width: 640px;
    }
     div[class=""super-header-wrap""] {
         width: 670px;
         width: 670px !important;
         max-width: 670px;
         margin: 0 auto;
    }
     div[class=""super-wrap""] {
         width: 670px;
         width: 670px !important;
         max-width: 670px;
         margin: 0 auto;
    }
     table[class*=""ad_content_670""] {
         width: 670px !important;
    }
     table[class*=""ad_content_670""] img {
         width: 640px !important;
    }
     table[class*=""no-text col-count-1""]{
        width: 670px !important;
    }
     table[class*=""no-text col-count-1""] a {
         width: 640px !important;
         display: block;
    }
     .full-table, .main-wrap, .header, .grid-table, .container, table[class=""full-table""], table[class*=""main-wrap""] {
         width: 670px !important;
         max-width: 670px;
    }
     #width-fix {
         width: 100% !important;
         min-width: 0 !important;
    }
     table[class=""columns-container""] table[class*=""ad_content_300""] img {
         width: 100%;
    }
     table[class*=""featured col-3""] table[class=""btn-outer-wrapper""], table[class*=""featured col-2""] table[class=""btn-outer-wrapper""] {
         margin: 0 auto 15px auto !important;
         float: none !important;
         width: 100%;
    }
     table[class*=""featured col-3""] img[class=""90x110""], table[class*=""featured col-2""] img[class=""90x110""], table[class*=""featured col-3""] img[class=""featured_lg""], table[class*=""featured col-2""] img[class=""featured_lg""] {
         display: block;
    }
     table[class*=""col-3""] img[class=""featured_lg""], table[class*=""col-2""] img[class=""featured_lg""] {
         float: none !important;
    }
     table[class*=""col-3""] h2[class=""title""], table[class*=""col-2""] h2[class=""title""], table[class*=""col-3""] div[class=""body""], table[class*=""col-2""] div[class=""body""], table[class*=""col-3""] img[class=""featured_lg""], table[class*=""col-2""] img[class=""featured_lg""] {
         margin-left: auto !important;
         margin-right: auto !important;
         width: 90% !important;
         display: block;
         padding: 0;
    }
}
/* Constrain email width for small screens */
@media screen and (max-width: 680px) {
     table[class*=""col ""] {
         min-height: 0 !important;
    }
     .art-col {
        /*display: table !important;
        */
    }
     div[class*=""force-col""]{
         border-bottom:2px solid #cecece;
         overflow: hidden;
    }
     table[yahoo] .force-col table[class*=""featured""]{
        min-width:0 !important;
    }
     table[class=""ad-container""] .force-col {
         border-bottom: none;
    }
     div[class*=""force-col last""]{
         border-bottom-width: 0;
    }
     table.columns-container, table.ad-container {
         border-right: none !important;
         border-left: none !important;
    }
    /* force container columns to (horizontal) blocks */
     td[class*=""force-col""], table[yahoo] .force-col {
         display: block !important;
         padding-right: 0 !important;
         height: auto !important;
         border-right: none !important;
         border-left: none !important;
         width: 100% !important;
    }
     td[class*=""force-col""] table[class*=""art-col""], table[yahoo] .force-col table[class*=""art-col""] {
         height: auto !important;
         width: 100% !important;
    }
     table[class*=""col ""] {
        /* unset table align=""left/right"" */
         float: none !important;
         width: 100% !important;
         height: auto !important;
        /* change left/right padding and margins to top/bottom ones */
        /*margin-bottom: 12px;
        */
         padding-bottom: 12px;
    }
     table[yahoo] table.col.ad {
         border-left: none !important;
         border-right: none !important;
    }
     table[yahoo] table.col.ad td {
         width: 100% !important;
    }
     table[yahoo] table.col.ad h3 {
         width: 100% !important;
         display: block;
         margin: 5px auto !important;
    }
     img[class=""featured_lg""] {
         float: none !important;
         display: block;
         margin: 0 auto 20px auto !important;
    }
     table[yahoo] table.col.ad a {
         margin: 0 auto;
    }
     table[yahoo] table.col.ad img {
         width: 100% !important;
         max-width: 300px !important;
         display: block;
    }
     table[yahoo] table.col.ad.ad_content_468 img {
         max-width: 100% !important;
         width: initial !important;
    }
     table[yahoo] table.col.ad.ad_content_670 img {
         max-width: 100% !important;
    }
     table[class*=""col last""] {
         border-bottom: none !important;
         margin-bottom: 0;
    }
    /* remove bottom border for last column/row */
     table[id*=""last-col-3""] {
         border-bottom: none !important;
         margin-bottom: 0;
    }
    /* align images right and shrink them a bit */
     img[class*=""col-3-img""] {
         float: right;
         margin-left: 6px;
         max-width: 130px;
    }
     .grid-list {
         height: 120px;
    }
     .grid-list li {
         width: 40%;
         float: left;
    }
    /*.ad-container .col-3.ad_content_300 td img {
        width: 400px;
    }
    */
     table[class*=""ad_content_670""] {
         width: 100% !important;
         min-width: 330px;
         padding: 0 !important;
    }
     table[class*=""ad_content_670""] img {
         width: 100%;
         height: auto;
    }
     table[class=""ad-container""] .ad {
         width: 100%;
    }
     .main-table-nobkgd {
         width: 400px;
    }
     .main-table-nobkgd td img {
         width: 400px;
         float: left;
    }
    /*.ad-container .col-3 {
        display: none;
    }
    */
     .nav li:last-child, .nav li:nth-child(4) {
         display: none;
    }
     .header h1 {
         font-size: 21px;
         line-height: 21px;
    }
     .header p {
         font-size: 12px;
    }
     .nav li:nth-child(3) {
         border-right: none;
    }
     .nav li {
         padding: 0 40px 0 40px;
    }
     table[class*=""featured-links""] td {
         font-size: 15px !important;
    }
     table[class*=""featured""] table[class=""btn-outer-wrapper""], table[class*=""featured""] table[class=""btn-inner-wrapper""] {
         width: auto !important;
    }
     table.ad-container table.art-col.ad.ad_content_180 img {
         margin: 0 60px !important;
    }
     table.ad-container table.art-col.ad.ad_content_180.no-text img {
         margin: 0 auto !important;
    }
     table.art-col.ad.no-text img {
        margin: 0 auto !important;
    }
     table.art-col.ad.no-text table {
        width: 100%;
    }
}
/* Give content more room on mobile */
@media screen and (max-width: 480px) {
     table[class=""container""], table[class=""grid-table""], table[class=""header""], table[yahoo].main-wrap, table[yahoo] .main-wrap {
         width: 330px !important;
    }
     div[class*=""force-col""]{
         border-bottom:2px solid #cecece;
         overflow: hidden;
    }
     div[class*=""force-col last""]{
         border-bottom-width: 0;
    }
     table[class*=""col ""] {
         min-height: 0 !important;
    }
     table.columns-container, table.ad-container {
         border-right: none !important;
         border-left: none !important;
    }
     td[class*=""force-col""], table[yahoo] .force-col {
         display: block !important;
         padding-right: 0 !important;
         height: auto !important;
         border-right: none !important;
         border-left: none !important;
         width: 100% !important;
    }
     table[yahoo] .force-col table[class*=""featured""]{
         min-width:0 !important;
    }
     table[yahoo] table.art-col.ad.no-text table {
         width: 300px !important;
    }
     table[yahoo] table.ad-container table.art-col.ad.ad_content_180.no-text h3, table[yahoo] table.ad-container table.art-col.ad.ad_content_300.no-text h3, table[yahoo] table.ad-container table.art-col.ad.ad_content_468.no-text h3 {
         min-width: 0 !important;
    }
     table[yahoo] table.col.ad td {
         width: 100% !important;
    }
     .col-count-1.col-i-1 table.featured div.body-container{
         margin: 0 !important;
    }
     table[yahoo] table.col.ad h3 {
         max-width: 300px;
    }
     img[class=""90x110""] {
         margin: 0 10px 0 15px !important;
    }
     td[class*=""force-col""] table[class*=""art-col""], table[yahoo] .force-col table[class*=""art-col""] {
         height: auto !important;
         width: 100% !important;
    }
     .p-grid, .title-grid {
         width: 300px;
    }
     .main-table-nobkgd {
         width: 320px;
    }
     .main-table-nobkgd td img {
         width: 320px;
         float: left;
    }
    /*.ad-container .col-3 {
        display:none;
    }
    */
     .nav li {
         padding: 0 20px 0 20px;
    }
     .nav li:nth-child(3) {
         border-right: none !important;
    }
     .grid-list li {
         padding-right: 20px;
    }
     table[class=""btn-inner-wrapper""] {
         width: 100% !important;
         padding: 0 !important;
         float: none !important;
    }
     table[class=""btn-outer-wrapper""] {
         padding: 0 !important;
         margin: 0 auto 10px auto !important;
         float: none !important;
         width: 100% !important;
    }
     table[class*=""featured""] table[class=""btn-inner-wrapper""] {
         width: 100% !important;
         float: none !important;
    }
     h2[class=""title""], div[class=""body""], img[class=""featured_lg""] {
         margin-left: auto !important;
         margin-right: auto !important;
         width: 90% !important;
         display: block;
         float: none !important;
    }
     table[class=""columns-container""] table[class*=""ad_content_300""] img {
         width: 100%;
         float: none;
    }
     *[class=""col last col-1""] img[class=""featured_lg""] {
         margin-right: 0;
    }
     table.ad-container table.art-col.ad.ad_content_180 img {
         margin: 0 60px !important;
    }
     td[class=""date""] {
         display: none;
         width: 0;
    }
}
 ;
/* NEXT STYLE */
 .toclink {
     color:#004750 !important;
}
 .advertisement a {
     color: #004750;
}
 .advertisement_r2 a {
     color: #0000FF;
}
 .advertisement_r2.featured {
     background-color: #E4E4E4 !important;
}
 .advertisement_r2.featured .advertisement_r2_content, .advertisement_r2.featured .advertisement_r2_content p, .advertisement_r2.featured .advertisement_r2_content h3 {
     font-family: Arial, 'Helvetica Neue', Helvetica, sans-serif !important;
     .linkcolor {
         color: #004750;
    }
     table.footer p {
         margin: 10px 0;
    }
     ;
    /* NEXT STYLE */
     @media print{
         #_t {
             background-image: url('https://oqrfrsq2.emltrk.com/oqrfrsq2?p&d=thabing@gmail.com&t=71379+46849916');
        }
    }
     div.OutlookMessageHeader {
        background-image:url('https://oqrfrsq2.emltrk.com/oqrfrsq2?f&d=thabing@gmail.com&t=71379+46849916')
    }
     table.moz-email-headers-table {
        background-image:url('https://oqrfrsq2.emltrk.com/oqrfrsq2?f&d=thabing@gmail.com&t=71379+46849916')
    }
     blockquote #_t {
        background-image:url('https://oqrfrsq2.emltrk.com/oqrfrsq2?f&d=thabing@gmail.com&t=71379+46849916')
    }
     #MailContainerBody #_t {
        background-image:url('https://oqrfrsq2.emltrk.com/oqrfrsq2?f&d=thabing@gmail.com&t=71379+46849916')
    }
     ;
    /* NEXT STYLE */
     
";


        [TestMethod]
        public void TestNUglify()
        {

            var result = Uglify.Css(badCss);

            Assert.IsTrue(result.HasErrors);

            var result2 = Uglify.Css(goodCss);

            Assert.IsFalse(result2.HasErrors);

            var result3 = Uglify.Css(bigCss);

        }

        [TestMethod]
        public void TestAngleSharpCss()
        {
            var ap = new AngleSharp.Css.Parser.CssParser();

            var sheet1 = ap.ParseStyleSheet(badCss);
            Assert.AreEqual(1, sheet1.Rules.Length);
            var result1 = sheet1.ToCss();
            Assert.IsNotNull(result1);

            var sheet2 = ap.ParseStyleSheet(goodCss);
            Assert.AreEqual(2, sheet2.Rules.Length);
            var result2 = sheet2.ToCss();
            Assert.IsNotNull(result2);

            var sheet3 = ap.ParseStyleSheet(bigCss);
            Assert.IsTrue(sheet3.Rules.Length > 1);
            foreach (ICssRule rule in sheet3.Rules.Where(r => r is ICssStyleRule).OrderByDescending(sr => ((ICssStyleRule)sr).Selector.Specificity))
            {
                Debug.Print($"{((ICssStyleRule)rule).Selector.Specificity} {rule.CssText}");
            }
            var result3 = sheet3.ToCss();
            Assert.IsNotNull(result3);

            var name = "a:test"; // a name with colon 'a:test' fails using ExCSS, but hopefully parses OK here
            var value = "color: red; margin:5px; padding: 10px;";
            var sSheet = ap.ParseStyleSheet($"{name} {{{value}}}");

            var newStyle = ((ICssStyleRule)sSheet.Rules.Single()).Style.CssText;
            Assert.IsFalse(string.IsNullOrWhiteSpace(newStyle)); //the style parses OK
            Assert.IsTrue(string.IsNullOrEmpty(((ICssStyleRule)sSheet.Rules.Single()).SelectorText)); //but the selector is empty

            name = "atest"; // a name with colon 'a:test' fails using ExCSS, but hopefully parses OK here
            value = "color: red; margin:5px; padding: 10px;";
            sSheet = ap.ParseStyleSheet($"{name} {{{value}}}");

            newStyle = ((ICssStyleRule)sSheet.Rules.Single()).Style.CssText;
            Assert.IsFalse(string.IsNullOrWhiteSpace(newStyle)); //the style parses OK
            Assert.AreEqual(name, ((ICssStyleRule)sSheet.Rules.Single()).SelectorText); //and the selector is OK
        }

        [TestMethod]
        public void TestExCSS()
        {


            var parser = new StylesheetParser();


            Stylesheet? stylesheet = null;
            var stylesheetTask = Task.Run<Stylesheet>(() => parser.Parse(badCss));
            var done = stylesheetTask.Wait(700);
            if (done)
                stylesheet = stylesheetTask.Result;

            Assert.IsNull(stylesheet);

            Stylesheet? stylesheet2 = null;
            var stylesheet2Task = Task.Run<Stylesheet>(() => parser.Parse(goodCss));
            var done2 = stylesheet2Task.Wait(700);
            if (done2)
                stylesheet2 = stylesheet2Task.Result;

            Assert.IsNotNull(stylesheet2);

            Assert.IsFalse(stylesheetTask.IsCompleted);
            Assert.IsTrue(stylesheet2Task.IsCompleted);

            var name = "a:test"; // a name with colon 'a:test' will not parse styles rules
            var value = "color: red; margin:5px; padding: 10px;";
            var sSheet = parser.Parse($"{name} {{{value}}}");
            Assert.AreEqual(0, sSheet.StyleRules.Count());


            name = "test";
            value = "color: red; margin:5px; padding: 10px;";
            sSheet = parser.Parse($"{name} {{{value}}}");
            var newStyle = sSheet.StyleRules.Single().Style.ToCss();
            Assert.IsFalse(string.IsNullOrEmpty(newStyle));

        }


    }
}
