import { AlertTriangle, RefreshCw } from "lucide-react";
import { Component, type ErrorInfo, type ReactNode } from "react";
import { Button } from "@/components/ui/button";

interface Props {
  children: ReactNode;
  fallback?: (error: Error, reset: () => void) => ReactNode;
}

interface State {
  error: Error | null;
}

/**
 * Top-level error boundary so a single bad render doesn't blank the SPA.
 * Logs the failure once to the console and shows a recovery UI; pressing
 * "Try again" clears the boundary state, which re-mounts the children.
 */
export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error("[ErrorBoundary]", error, info.componentStack);
  }

  reset = () => this.setState({ error: null });

  render() {
    if (!this.state.error) return this.props.children;

    if (this.props.fallback) {
      return this.props.fallback(this.state.error, this.reset);
    }

    return (
      <div role="alert" className="flex min-h-screen items-center justify-center bg-muted/40 px-4">
        <div className="w-full max-w-md space-y-4 rounded-lg border bg-card p-6 shadow-sm">
          <div className="flex items-center gap-2 text-destructive">
            <AlertTriangle className="h-5 w-5" />
            <h2 className="text-lg font-semibold">Something went wrong</h2>
          </div>
          <p className="text-sm text-muted-foreground">
            The dashboard hit an unexpected error and couldn't render this view.
          </p>
          <pre className="max-h-40 overflow-auto rounded-md bg-muted/40 p-3 font-mono text-xs">
            {this.state.error.message}
          </pre>
          <div className="flex gap-2">
            <Button onClick={this.reset}>
              <RefreshCw className="mr-1 h-4 w-4" />
              Try again
            </Button>
            <Button variant="outline" onClick={() => window.location.reload()}>
              Reload page
            </Button>
          </div>
        </div>
      </div>
    );
  }
}
