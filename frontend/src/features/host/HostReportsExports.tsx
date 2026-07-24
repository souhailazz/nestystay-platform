import { useState } from "react";
import { Download, FileText, FileSpreadsheet } from "lucide-react";
import { PatoisPhrase } from "../../lib/patois";

interface HostReportsExportsProps {
  view: string;
  token: string;
}

export function HostReportsExports({ view, token }: HostReportsExportsProps) {
  const isExports = view === "exports";

  function downloadCSV(type: string) {
    const csvData = "data:text/csv;charset=utf-8,ID,Property,Amount,Tax,Date\n1,Ocho Rios Villa,185.00,27.75,2026-07-20\n";
    const link = document.createElement("a");
    link.href = encodeURI(csvData);
    link.download = `nesty-${type}-report-2026.csv`;
    document.body.appendChild(link);
    link.click();
    link.remove();
  }

  return (
    <div className="page-container container py-6" data-testid={isExports ? "host-11-page" : "host-10-page"} id={isExports ? "HOST-11" : "HOST-10"}>
      <header className="page-header mb-6">
        <span className="badge badge-sun">{isExports ? "HOST-11" : "HOST-10"}</span>
        <h2>{isExports ? "Data Exports & CSV Reports" : "Monthly Host Statement & Reports"}</h2>
        <PatoisPhrase phrase="Financial Exports & Accounting" translation="Download itemized monthly statements, tax documentation, and CSV data." />
      </header>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 max-w-4xl">
        <div className="card-box">
          <div className="flex items-center gap-3 mb-3">
            <FileSpreadsheet size={24} className="text-green" />
            <div>
              <h3 className="font-bold">Revenue & Earnings CSV</h3>
              <p className="subtext">Itemized revenue breakdown by property and month.</p>
            </div>
          </div>
          <button type="button" className="btn btn-outline w-full" onClick={() => downloadCSV("revenue")}>
            <Download size={16} /> Download Revenue CSV
          </button>
        </div>

        <div className="card-box">
          <div className="flex items-center gap-3 mb-3">
            <FileSpreadsheet size={24} className="text-blue" />
            <div>
              <h3 className="font-bold">Bookings & Reservations CSV</h3>
              <p className="subtext">All guest bookings, check-in dates, and payment statuses.</p>
            </div>
          </div>
          <button type="button" className="btn btn-outline w-full" onClick={() => downloadCSV("bookings")}>
            <Download size={16} /> Download Bookings CSV
          </button>
        </div>

        <div className="card-box">
          <div className="flex items-center gap-3 mb-3">
            <FileSpreadsheet size={24} className="text-sun" />
            <div>
              <h3 className="font-bold">Tax Documentation CSV (GCT 15%)</h3>
              <p className="subtext">Tax records for Jamaican tax compliance.</p>
            </div>
          </div>
          <button type="button" className="btn btn-outline w-full" onClick={() => downloadCSV("tax")}>
            <Download size={16} /> Download Tax Statement
          </button>
        </div>

        <div className="card-box">
          <div className="flex items-center gap-3 mb-3">
            <FileText size={24} className="text-purple" />
            <div>
              <h3 className="font-bold">Monthly Summary PDF Statement</h3>
              <p className="subtext">Comprehensive PDF report for monthly earnings.</p>
            </div>
          </div>
          <button type="button" className="btn btn-primary w-full" onClick={() => window.print()}>
            <Download size={16} /> Print / Export PDF Statement
          </button>
        </div>
      </div>
    </div>
  );
}
